Meka Programming Language
===========================
The Meka programming language is a combination of an Object Oriented Knowledge Database system with
a constraint and rule based Artificial Intelligence programming language designed for the easy creation
of Expert Systems, Natural Language Processing (NLP) engines and other Artificial Intelligence related
applications

Knowledge DataBase (KDB)
---------------------------
An object oriented knowledge representation system.

Example:

````
object Human:
	var(property) Alive
	var(property) Mortal

object John is [Human]:
	var(string) Name = "John Down"
	var(property) Code
````	
	
Interpretation:
````
John is a human whose name is John Down and he can code. Like all humans, John is alive and is mortal.
````
