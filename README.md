# Hide and Seek-Unity environment for Reinforcement Learning
Hide and Seek environment is a project built with [Unity](https://unity.com/) and [ML-Agents](https://github.com/Unity-Technologies/ml-agents). 
It intends for neuroscience inspired reinforcement learning. This project includes Unity assets and an example OpenAI Gym environment.  
## Features
### Unity assets
Unity assets support custimizing terrain, rocks and players in Unity Editor.
### OpenAI Gym environment
OpenAI Gym environment can be used with ML-Agents for training directly.
#### Parameters
- 3 Hiders for training
- 3 Seekers with naive policy. They set a destination and obtain path from `NavMesh`. The destination is random if no hider is seen, 
otherwise set to the last seen hider's position. 
- Hiders' epsisode ends when they are caught, and seekers epsisode ends all hiders are caught. When a hider is caught, it'll be destroyed. When all hiders are caught,
all players will be respawned.
- A camera is attached to each players to display its field of view.
- They game view is consist of a top down view, field of views for all players. The top row is for hiders, and bottom row for seekers. The camera view will blackout
when a player is destroyed.
#### Control
- Turning left and right
- Going forward and backward
## Instruction
### OpenAI Gym environment
1. Install [ML-Agents](https://github.com/Unity-Technologies/ml-agents)
2. Run `mlagents-learn <config path> --env <env path> --run-id <run id>`. `<config path>` is a training configuration, 
an example is `\Training\Config\Hider.yaml`. `<env path>` is the path of an envionment, an example is `\Env\Hide and Seek.exe`. `<run id>` is an id set for training, 
e.g. HAS_1.
3. Press `Alt`+`Enter` to enter full screen
4. Training is at 20x speed.
## Problem to fix
- Producural generation may spawn rocks on the air
- Players in navigation mode with `NavMesh` ignore players' colliders.
