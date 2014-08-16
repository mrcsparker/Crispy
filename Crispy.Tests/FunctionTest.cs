using NUnit.Framework;

using System;
using System.Dynamic;
using System.IO;
using System.Reflection;
using Newtonsoft;

namespace Crispy.Tests
{
    [TestFixture]
    public class FunctionTest
    {
        public delegate void Action();

        [Test]
        public void ShouldBeAbleToCreateASimpleFunction()
        {

            const string text = @"
import System.Console as console
import System.Math as math
import System.Collections as collections

// Prints output
defun print(str) {
    console.WriteLine(str)
}

defun princ(str) {
    console.Write(str)
}

defun array() {
    new collections.ArrayList()      
}

var x = array()

x.add('xxx')
x.add(2)
x.add('abc')

print(x[0])

x[0] = 'yyy'

var add2 = lambda(x, y) {
    x + y
}

add2(3, 4)

defun map(fn, a) {

    var i = 0

    loop {
        a[i] = fn(a[i])

        i = i + 1

        if (i == a.count) {
            break
        }     
    }
}

var a = array();
a.add(1)
a.add(2)
a.add(3)

map(lambda(x) { x * 2 }, a);

defun printArr(a) {
    i = 0

    loop {

        print(a[i])

        i = i + 1

        if (i == a.count) {
            break(a)
        }        
    }
}

var x = printArr(a)
x

defun foo() {
    15
}

var y = foo();

";

            const string text5 = @"

import System.Dynamic as dynamic
import Newtonsoft.Json as json

defun objn() {
    new dynamic.ExpandoObject()
}

var foo = objn();
foo.Bar = 'something'

var out = json.JsonConvert.SerializeObject(foo);
";

            string dllPath = typeof(object).Assembly.Location;
            Assembly asm = Assembly.LoadFile(dllPath);

            string dynamicObjectPath = typeof(System.Dynamic.ExpandoObject).Assembly.Location;
            Assembly dynamicObjectAsm = Assembly.LoadFile(dynamicObjectPath);

            string jsonObjectPath = typeof(Newtonsoft.Json.JsonConvert).Assembly.Location;
            Assembly jsonAsm = Assembly.LoadFile(jsonObjectPath);

            Console.WriteLine(asm);

            var Crispy = new Crispy(new[] {asm, dynamicObjectAsm, jsonAsm});
            var output = Crispy.ExecuteExpr(text, new ExpandoObject());
            Console.WriteLine(output);

        }
    }
}

/*
function add(a,b,c,d,e) then
                var comment = 'this is inside'
                a + b
            end

            add(10, 1000, 1, 2, 3)
*/
