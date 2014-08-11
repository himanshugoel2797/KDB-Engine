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
