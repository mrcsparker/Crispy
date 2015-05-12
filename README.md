Crispy
======

## Introduction

Crispy is a functional programming language that targets the .NET DLR.  It targets both the MS .NET runtime and the Mono .net runtime.  It was developed using Mono on Mac OS X and runs on Windows, Mac OS X, and GNU/Linux.

Crispy compiles down to .NET bytecode, so it is very, very fast.

## License

Crispy uses a MIT License, which means that you can do what you want with it:

+ Use it to learn about the .NET DLR
+ Use it in your FOSS program
+ Use it in your proprietary program

Please send me any bug fixes or enhancements.

## Syntax

Crispy is a simple language.

An example Crispy syntax would be:

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
	defun add(a, b) {
		a + b
	}
	
	// variables
	var foo = add(1, 2)

	// lambda expressions
	var add2 = lambda(x, y) {
    		x + y
	}

	add2(3, 4)
	
	// loops
	
	var start = 0
	var stop = 10
	loop {
		start += 1
		if (start == stop) {
			break
		}
	}
	
	// combine those, and you can add all sorts of things...
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
	
	var results = map(lambda(x) { x * 2 }, a);
	

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

	
