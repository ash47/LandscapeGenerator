LandscapeGenerator
==================

I implemented DiamondSquare landscape generation algorithm using SharpDX for the graphics and interaction subject.

For more detail on the project see project1.pdf

###Generating the height map###
 - I create a 2d array of floats big enough to fit all the points needed
 - You select the number of iterations to run, then this number will be auto generated based on that.
 - I set the 4 corners to a default height value
 - I run the Square() method, which does the following:
  - Calculates the height of the given point
  - Calculates the 4 diamonds around it
  - Calls itself 4 times in order to calculate the 4 squares
  - It keeps track of how many iterations it has done, via a size variable, so it eventually stops

###Building the mesh###
 - I simply use a double for loop (one for x, one for y) to loop over the height map array
 - I know the x and y coordinates, they are calculated based on the x and y in the loop, and scaled based on the scalers
 - I pull the z values from the height map, and then scale them based on the scalers
 - Once the terrain has been built, I add two more triangles for the water, the water spans across the entire terrain at a given height

###My Awesome Controls###
 - Controls are up to me, they are similar to FPS controls, however they have been made harder to use, just for your entertainment
 - WADS will allow you to move forward and straf
 - Q and Z will allow you to move up and down
 - You can look around with the mouse, again, the mouse controls were by design ;)
 - Press escape to quit

###Constants###
 - There are a ton of constants at the top of Landscape.cs, they are pretty fun to play around with, but I assume most people would have these.
