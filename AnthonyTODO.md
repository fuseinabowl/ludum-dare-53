# Track building

- Only allow connecting "roots" of different types. Currently villages can be connected to villages and farms can be connected to farms.
- Bridges
 - nope
- Only allow tracks to be built between non-sharp edges of a polygon
 - done by Ben

# Level building

- Single large level
- People are unlikely to replay our LD game, minimal bang for buck implementing proc gen

# Train moving

- Smoothing at ends aka acceleration
- Carriages
- Flipping direction of carriages at ends

# Economy

- Replace with simpler system
- You need to connect a number of sources to destinations to unlock another source
  - all hard coded as part of our level
- You have unlimited tracks
- You have unlimited bridges (if we have bridges)
- You are just trying to work out the puzzle of laying the tracks along the weird grid

# Gameify it

- Lots of scope creep
- Focus on the puzzle element of laying the tracks and on the zen element
- No additional game elements

# Glue

- Tutorial flow like prompts (may want other things like a tutorial fog)
- HUD
- Menus

# Fun stuff

- Stretch goals
  - Day/night cycle
  - Weather cycle

# Graphics

- Models, animations
- particles
- terrain textures
- transitions
  - between?

# Sound

- Music
- Effects

# Anthony's tasks

- Create level editor - Done!
- Make level editor able to rotate tiles - Done!
- Make level editor able to flip tiles
- Add visble placement indicator
- Create 1st level
  - Create models
    - need concept art
  - Create basic layout
- Add prop spawner
- Add enclosed "stations" for farms and for market drop offs
  - hides the train turning around + getting loaded/unloaded
- Fix grid relaxing
  - Will probably fix sides of the world
    - if it doesn't, trim the sides of the world
