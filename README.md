# Archipelago Terraria Client

Not to be confused with prior implementations:

* [TerrariaFlagRandomizer](https://github.com/Cronus-waters/TerrariaFlagRandomizer)
* [TerrariaArchipelago](https://github.com/Whoneedspacee/TerrariaArchipelago)

Archipelago is a multiworld randomizer, which is a sort of multi-game mod that shuffles things
between games. So, for example, when you kill Skeletron in your Terraria game, your friend
might get Mothwing Cloak in their Hollow Knight game. But you can't enter the Dungeon
until your friend picks up the item at Vengeful Spirit's location in their game. Archipelago
has support for a lot of games. Learn more on its [website](https://archipelago.gg/).

This implementations uses boss kills and event clears as checks. The items are the permanent
changes to the world that bosses and events reward in vanilla. Things like "Post-Plantera" and
"Hardmode". Bosses still drop their original loot, though. Optionally, you may also include
achievement checks and item rewards.

Because this mod adds an extra layer of multiplayer, there are many ways to use it:
* Randomize one singleplayer Terraria world
* Randomize one multiplayer Terraria world
* Randomize multiple singleplayer or multiplayer Terraria worlds between each other
* Randomize Terraria with entirely separate games

## Usage

### For the Terraria player

Subscribe to the mod at
[its Steam page](https://steamcommunity.com/sharedfiles/filedetails/?id=2922217554). In Workshop >
Manage Mods, edit Archipelago Randomizer (Seldom's Implementation)'s settings. Since this game
isn't on Archipelago's main server, you'll need to set all of the settings, including the advanced
ones. If you're hosting, the address will probably be "localhost" and the server should tell you
the port. If not, whoever's hosting should know.

You may use as many worlds as you like. When you're ready, run `/apstart` to enable sending
and receiving checks. You may open your world in multiplayer for others to join.

### For the host, if you have any Terraria players

You should have some technical knowledge to get through this part. Run
[my Archipelago fork](https://github.com/Seldom-SE/Archipelago/tree/terraria) from source (see this
[guide](https://github.com/Seldom-SE/Archipelago/blob/terraria/docs/running%20from%20source.md)).
You will need to run `Generate.py` and `MultiServer.py`. To create the `yaml` files, you can run
`WebHost.py`. This will run a website, so watch the console for an address including `localhost`,
and go there on your browser. Terraria players have to setup their `yaml` files from this website,
but other games should work fine from [the official site](https://archipelago.gg/), unless I
haven't kept my fork up to date. You may also host the game from `WebHost.py`, if you prefer.

## Future Work

- [X] Remove effects from original bosses
- [X] Re-add effects conditionally
- [X] Announce boss / event clears to server
- [X] Client responses to server
- [X] Server-side implementation
- [X] Terraria multiplayer
- [X] Prevent accidental checks when loading older worlds
- [X] Wall, Plantera, and Zenith goals (maybe more)
- [X] Refactor!
- [ ] More thorough chat integration
- [X] Work on the README
- [X] Publish to Steam Workshop. Makes setup so much easier
- [X] Rebalance items
- [X] Explore more item/location options, like achievements, NPCs, bestiary completion, Journey
research, and item rewards
- [X] Docs
- [ ] Get my fork merged if people want it
- [ ] Other Archipelago features like deathlink? Do people want deathlink?
- [ ] Calamity maybe probably maybe

## License

Archipelago Terraria Client is licensed under MIT.
