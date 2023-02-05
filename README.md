# Hide and Seek-Unity environment for Reinforcement Learning
Hide and Seek environment is a project built with [Unity](https://unity.com/) and [ML-Agents](https://github.com/Unity-Technologies/ml-agents). 
It intends for neuroscience inspired reinforcement learning. This project includes Unity assets and an example of OpenAI Gym environment.  
<p align="center">
  <img width="432" alt="Scene" src="https://user-images.githubusercontent.com/38318509/216796626-836d619d-d017-46e0-92b8-23016fa6bba4.png">


## Features
### Customizable Game Objects. 
Unity assets support customizable terrain, rocks and players in Unity Editor.
#### Customizable terrain and rocks 
- Sharpness and roughness of terrain
- Size of terrain
- Number of rocks
- Size of rocks
- Seed
  
  <img width="450" alt="Terrain Roughness" src="https://user-images.githubusercontent.com/38318509/216796848-b8fb5267-43b6-4d7a-ad32-7ca44fab95e6.png"><img width="450" alt="terrain seed and num rocks" src="https://user-images.githubusercontent.com/38318509/216796850-ecfd0410-a0d8-4191-afaf-6205ff1e3968.png">

#### Customizable attributes for players 
- Speed
- range of field of view
- type of field of view (first person & top-down)
- number of step to freeze when episodes begin, so hiders have time for preparation
  <p align="center">
  <img width="450" alt="Player speed   FOV" src="https://user-images.githubusercontent.com/38318509/216796856-553a981a-d535-4eeb-a186-b9f5fb2f9bbe.png">

### Control
#### Steering Mode (Support human control by keyboard)
- Turning left(&larr;) and right(&rarr;)
- Going forward(&uarr;) and backward(&darr;)
#### Destination Mode
- Divide the map into  n x n grids
- Select a grid from top-down view, ```NavMesh``` will take the agent to there
#### Detection Mode (Seeker only)
- If a hider appear in a seeker's first-person view, the seeker will go to the hider's position
- If no hider in a seeker's first-person view, the seeker will go to the hider's last seen position
- If a seeker has always arrived at a hider's last seen position and no hider is in its field of view, it'll choose a random location
