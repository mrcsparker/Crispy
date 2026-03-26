using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Crispy.Parsing;

namespace Crispy.Repl
{
    public sealed class ReplSession
    {
        private static readonly Regex PersistentVarPattern = new(
            @"^\s*var\s+(?<name>[@A-Za-z_][@A-Za-z0-9_]*)\s*(?:=\s*(?<value>.+?))?\s*;?\s*$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex PersistentAssignmentPattern = new(
            @"^\s*(?<name>[@A-Za-z_][@A-Za-z0-9_]*)\s*=\s*(?<value>.+?)\s*;?\s*$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private readonly Assembly[] _assemblies;
        private readonly object[] _instanceObjects;
        private readonly StringBuilder _buffer = new();
        private HashSet<string> _baselineGlobalNames = new(StringComparer.OrdinalIgnoreCase);
        private CrispyRuntime _runtime;
        private ExpandoObject _scope;

        public ReplSession()
            : this(CreateDefaultAssemblies(), [])
        {
        }

        public ReplSession(Assembly[] assemblies)
            : this(assemblies, [])
        {
        }

        public ReplSession(Assembly[] assemblies, object[] instanceObjects)
        {
            ArgumentNullException.ThrowIfNull(assemblies);
            ArgumentNullException.ThrowIfNull(instanceObjects);

            _assemblies = [.. assemblies];
            _instanceObjects = [.. instanceObjects];
            _runtime = CreateRuntime();
            _scope = CrispyRuntime.CreateNamespace();
            RefreshBaselineGlobalNames();
        }

        public string Prompt
        {
            get
            {
                return _buffer.Length > 0 ? ".... " : "crispy> ";
            }
        }

        public ReplSubmissionResult SubmitLine(string line)
        {
            ArgumentNullException.ThrowIfNull(line);

            var trimmed = line.Trim();
            if (trimmed.Length == 0 && _buffer.Length == 0)
            {
                return ReplSubmissionResult.None();
            }

            if (trimmed.StartsWith(':'))
            {
                return HandleCommand(trimmed);
            }

            if (_buffer.Length == 0 &&
                TryHandlePersistentBinding(line, out var persistentResult))
            {
                return persistentResult;
            }

            AppendLine(line);
            var source = _buffer.ToString();

            try
            {
                var value = _runtime.ExecuteExpr(source, _scope);
                _buffer.Clear();
                return ReplSubmissionResult.Executed(value, FormatValue(value));
            }
            catch (ParserException ex) when (ShouldBuffer(source, ex))
            {
                return ReplSubmissionResult.Incomplete();
            }
            catch (TokenizerException ex) when (ShouldBuffer(source, ex))
            {
                return ReplSubmissionResult.Incomplete();
            }
            catch (Exception ex)
            {
                _buffer.Clear();
                return ReplSubmissionResult.Error(FormatException(ex));
            }
        }

        private static Assembly[] CreateDefaultAssemblies()
        {
            Assembly[] assemblies =
            [
                typeof(object).Assembly,
                typeof(ExpandoObject).Assembly
            ];
            return [.. assemblies.Distinct()];
        }

        private CrispyRuntime CreateRuntime()
        {
            return _instanceObjects.Length == 0
                ? new CrispyRuntime(_assemblies)
                : new CrispyRuntime(_assemblies, _instanceObjects);
        }

        private void ResetSession()
        {
            _buffer.Clear();
            _runtime = CreateRuntime();
            _scope = CrispyRuntime.CreateNamespace();
            RefreshBaselineGlobalNames();
        }

        private void RefreshBaselineGlobalNames()
        {
            _baselineGlobalNames = ((IDictionary<string, object?>)_runtime.Globals)
                .Keys
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private void AppendLine(string line)
        {
            if (_buffer.Length > 0)
            {
                _buffer.AppendLine();
            }

            _buffer.Append(line);
        }

        private ReplSubmissionResult HandleCommand(string trimmed)
        {
            if (trimmed.Equals(":quit", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals(":exit", StringComparison.OrdinalIgnoreCase))
            {
                return ReplSubmissionResult.Exit();
            }

            if (trimmed.Equals(":help", StringComparison.OrdinalIgnoreCase))
            {
                return ReplSubmissionResult.Info(
                    "Commands:\n" +
                    ":help   Show this help.\n" +
                    ":clear  Discard the current multiline submission.\n" +
                    ":reset  Reset the session scope and runtime.\n" +
                    ":scope  List names introduced in the current session.\n" +
                    ":load <path>  Load a Crispy file as a module into the session.\n" +
                    ":quit   Exit the REPL.\n\n" +
                    "Multiline input continues automatically while a submission looks incomplete.");
            }

            if (trimmed.Equals(":clear", StringComparison.OrdinalIgnoreCase))
            {
                var hadBuffer = _buffer.Length > 0;
                _buffer.Clear();
                return ReplSubmissionResult.Info(hadBuffer
                    ? "Cleared buffered input."
                    : "No buffered input.");
            }

            if (trimmed.Equals(":reset", StringComparison.OrdinalIgnoreCase))
            {
                ResetSession();
                return ReplSubmissionResult.Info("Session reset.");
            }

            if (trimmed.Equals(":scope", StringComparison.OrdinalIgnoreCase))
            {
                var sessionNames = GetSessionNames();
                return sessionNames.Length == 0
                    ? ReplSubmissionResult.Info("No session bindings.")
                    : ReplSubmissionResult.Info(string.Join(Environment.NewLine, sessionNames));
            }

            if (trimmed.StartsWith(":load ", StringComparison.OrdinalIgnoreCase))
            {
                if (_buffer.Length > 0)
                {
                    return ReplSubmissionResult.Error("Cannot load a file while a multiline submission is buffered. Use :clear first.");
                }

                var path = trimmed[6..].Trim();
                return path.Length == 0
                    ? ReplSubmissionResult.Error("Usage: :load <path>")
                    : LoadFile(path);
            }

            return ReplSubmissionResult.Error("Unknown command. Type :help for available commands.");
        }

        private bool TryHandlePersistentBinding(string line, out ReplSubmissionResult result)
        {
            result = ReplSubmissionResult.None();

            var varMatch = PersistentVarPattern.Match(line);
            if (varMatch.Success)
            {
                return TryEvaluatePersistentBinding(
                    varMatch.Groups["name"].Value,
                    varMatch.Groups["value"].Success ? varMatch.Groups["value"].Value : null,
                    out result);
            }

            var assignmentMatch = PersistentAssignmentPattern.Match(line);
            return assignmentMatch.Success &&
                TryEvaluatePersistentBinding(
                    assignmentMatch.Groups["name"].Value,
                    assignmentMatch.Groups["value"].Value,
                    out result);
        }

        private bool TryEvaluatePersistentBinding(string name, string? expressionText, out ReplSubmissionResult result)
        {
            try
            {
                var value = expressionText == null
                    ? null
                    : _runtime.ExecuteExpr(expressionText, _scope);
                ((IDictionary<string, object?>)_scope)[name] = value;
                result = ReplSubmissionResult.Executed(value, FormatValue(value));
                return true;
            }
            catch (ParserException ex) when (expressionText != null && ShouldBuffer(expressionText, ex))
            {
                result = ReplSubmissionResult.None();
                return false;
            }
            catch (TokenizerException ex) when (expressionText != null && ShouldBuffer(expressionText, ex))
            {
                result = ReplSubmissionResult.None();
                return false;
            }
            catch (Exception ex)
            {
                result = ReplSubmissionResult.Error(FormatException(ex));
                return true;
            }
        }

        private string[] GetSessionNames()
        {
            return
            [
                .. ((IDictionary<string, object?>)_scope)
                .Keys
                .Where(name => !_baselineGlobalNames.Contains(name))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            ];
        }

        private ReplSubmissionResult LoadFile(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                var alias = Path.GetFileNameWithoutExtension(fullPath);
                var module = _runtime.ExecuteFile(fullPath, alias);
                ((IDictionary<string, object?>)_scope)[alias] = module;
                return ReplSubmissionResult.Info("Loaded " + fullPath + " as " + alias + ".");
            }
            catch (Exception ex)
            {
                return ReplSubmissionResult.Error(FormatException(ex));
            }
        }

        private static bool ShouldBuffer(string source, Exception ex)
        {
            return HasOpenStructures(source) || IsEndOfInputError(ex);
        }

        private static bool HasOpenStructures(string source)
        {
            try
            {
                var tokenizer = new Tokenizer(new StringReader(source));
                var parenDepth = 0;
                var bracketDepth = 0;
                var blockDepth = 0;

                for (; ; )
                {
                    var token = tokenizer.NextToken();
                    if (token.Type == TokenType.End)
                    {
                        break;
                    }

                    switch (token.Type)
                    {
                        case TokenType.OpenParen:
                            parenDepth += 1;
                            break;
                        case TokenType.CloseParen:
                            parenDepth -= 1;
                            break;
                        case TokenType.OpenBracket:
                            bracketDepth += 1;
                            break;
                        case TokenType.CloseBracket:
                            bracketDepth -= 1;
                            break;
                        case TokenType.KeywordThen:
                            blockDepth += 1;
                            break;
                        case TokenType.KeywordEnd:
                            blockDepth -= 1;
                            break;
                    }
                }

                return parenDepth > 0 || bracketDepth > 0 || blockDepth > 0;
            }
            catch (TokenizerException ex)
            {
                return ex.Message.Contains("Quoted string was not terminated", StringComparison.Ordinal);
            }
        }

        private static bool IsEndOfInputError(Exception ex)
        {
            return ex.Message.Contains("Quoted string was not terminated", StringComparison.Ordinal) ||
                ex.Message.Contains("found End", StringComparison.Ordinal);
        }

        private static string FormatException(Exception ex)
        {
            ArgumentNullException.ThrowIfNull(ex);
            return ex.GetType().Name + ": " + ex.Message;
        }

        private static string FormatValue(object? value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is string text)
            {
                return text;
            }

            if (value is bool booleanValue)
            {
                return booleanValue ? "true" : "false";
            }

            if (value is ExpandoObject expando)
            {
                var names = string.Join(", ", ((IDictionary<string, object?>)expando).Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
                return names.Length == 0
                    ? "ExpandoObject {}"
                    : "ExpandoObject { " + names + " }";
            }

            return value is IDictionary dictionary
                ? value.GetType().Name + " (Count = " + dictionary.Count.ToString(CultureInfo.InvariantCulture) + ")"
                : value is ICollection collection
                ? value.GetType().Name + " (Count = " + collection.Count.ToString(CultureInfo.InvariantCulture) + ")"
                : value.GetType().Name.Equals("CrispyCallable", StringComparison.Ordinal)
                ? "<function>"
                : value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture) ?? value.ToString() ?? string.Empty
                : value.ToString() ?? string.Empty;
        }
    }
}
