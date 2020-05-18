Note: 
This serves as a good starting point/proof of concept, but the code itself is too clunky. I suggest rebuilding the codebase if this project is pursued.

Key Components:
-Adults, Childrens, and Elders, decisions are different as they have different amounts of attention span, extraversion, etc. Actions driven by personality traits and primitive desires(eg. hunger), also have happiness that changes based on if desires are being met by interactables
-Placable and movable interactables for NPCs to interact with
-GameState object that keeps a relative time scale for NPCs, can easily make and attatch scripts for analytics

Purpose:
By building a simulation consisting of actors that represent the expected demographic in a buliding, it will be easier to assess spatial efficiency. Users may experiment with different types or amounts of people and move objects around to find a proper layout that avoids congestion. Happiness is an analytic for each agent ranging from 0-100 that tries to give a numerical value to how a person is affected by a building layout. A lower number indicates that the person is getting stuck or having to move too much.

![Example Image](https://drive.google.com/file/d/1xA0-iIxj0VTh-6S6ZwKqTRdamR8jAeS0/view?usp=sharing)
