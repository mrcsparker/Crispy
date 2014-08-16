Crispy
======

## Introduction

Crispy is a functional programming language that targets the .NET DLR.  It is
currently a rule language in a larger project.

Crispy compiles down to .NET bytecode, so it is very, very fast.

## Syntax

Crispy is a simple language.  It was written to be a simple rule language that could use used by clients and CSTs, while being expressive enough for a developer to work with.

There are two ways for devs to use for Crispy: a simple, rules-based syntax and a more expressive syntax.  An example of the simple syntax would be:

	if (time == now) then
		send("ready")
	endif
	
	if (1 > 2) then
		if (a or b) then
			...
		endif
	elsif (3 < 4) then
		...
	else
		...
	endif

The other syntax has functions, variables, loops, arrays, lambda expressions, first class functions, namespaces, closures, and .NET integration.  

An example of the more advanced syntax would be:

	// arrays (well, hashes in this case)
	var x = array()

	x.add('xxx')
	x.add(2)
	x.add('abc')

	print(x[0])

	x[0] = 'yyy'

	// functions
	function add(a, b) {
		a + b
	}
	
	// variables
	var foo = add(1, 2)

	// lambda expressions
	var add2 = lambda(x, y) {
    	x + y
	}

	add2(3, 4)
	

## Crispy EBNF (old, will update)

	Expression =
		LogicalOrNode

	LogicalOrNode =
		LogicalAndNode { ( '||' 'OR' ) LogicalAndNode }

	LogicalAndNode =
		ComparisonNode {  ( '&&' 'AND' ) ComparisonNode } 

	ComparisonNode =
		AdditiveNode { ( '=' '==' '!=' '<>' '>' '>=' '<' '<=' ) AdditiveNode }

	AdditiveNode =
		MultiplicativeNode { ( '+' '-' ) MultiplicativeNode }

	MultiplicativeNode =
		UnaryNode { ( '*' '/' '%' 'MOD' '^' ) UnaryNode }

	UnaryNode =
		(( '-' '!' 'NOT' ) PrimaryNode ) UnaryNode
	
	PrimaryNode =
		IdentifierNode | StringLiteralNode | NumberLiteralNode | ParenExpressionNode
	
	IdentifierNode =
		FunctionCallNode

	StringLiteralNode =
		'"' ( Anything )* '"' | ''' ( Anything )* '''

	FunctionCallNode =
		Identifier '(' (ParenExpressionNode)? ')' 

	ParenExpressionNode =
		Expression ( ',' Expression ) *

	NumberLiteralNode =
		Number

	Identifier =
		Letter ( Letter | Digit | '_' )*

	Number =
		Digit * ( '.' Digit + )? (('e'|'E') ('+'|'-')? Digit + )?

	Letter =
		'a' .. 'z' | 'A' .. 'Z'

	Digit =
		'0' | '1' .. '9'

	Anything =
		Pretty much anything

	
