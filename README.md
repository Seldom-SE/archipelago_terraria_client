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

## Important Known Issue

For achievements to work, you must click Workshop > Manage Mods > Force Reload every time you start
the game.

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
- [ ] Work on the README
- [ ] Publish to Steam Workshop. Makes setup so much easier
- [X] Rebalance items
- [X] Explore more item/location options, like achievements, NPCs, bestiary completion, Journey research, and item rewards
- [ ] Docs
- [ ] Get my fork merged if people want it
- [ ] Other Archipelago features like deathlink? Do people want deathlink?
- [ ] Calamity maybe probably maybe

## Usage

### For the Terraria player

Clone/download this repo into Documents/My Games/Terraria/tModLoader/ModSources such that
SeldomArchipelago.sln is at
Documents/My Games/Terraria/tModLoader/ModSources/SeldomArchipelago/SeldomArchipelago.sln.
In tModLoader, go to Workshop > Develop Mods, and click "Build + Reload" under SeldomArchipelago.
For good measure, also click Workshop > Manage Mods > Force Reload. You'll have to do this
every time you start the game. In Workshop > Manage Mods, edit Archipelago Randomizer
(Seldom's Implementation)'s settings. Since this game isn't on Archipelago's main server,
you'll need to set all of the settings, including the advanced ones. If you're hosting, the address
will probably be "localhost" and the server should tell you the port. If not, whoever's hosting
should know.

You may use as many worlds as you like. Pre-existing worlds will require a command (it will prompt
you) to enable sending and recieving checks. There are many known issues when playing
on multiplayer.

### For the host, if you have any Terraria players

You should have some technical knowledge to get through this part. Run
[my Archipelago fork](https://github.com/Seldom-SE/Archipelago/tree/terraria) from source (see this
[guide](https://github.com/Seldom-SE/Archipelago/blob/terraria/docs/running%20from%20source.md)).
The command for that will look something like `python WebHost.py`. This will run a website,
so watch the console for an address including `localhost`, and go there on your browser.
You can find information for how to setup the game on the website. Terraria players have to setup
their `yaml` files from this website, but other games should work fine from
[the official site](https://archipelago.gg/), unless I haven't kept my fork up to date.

## License

Archipelago Terraria Client is licensed under MIT.
