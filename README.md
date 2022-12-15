# Archipelago Terraria Client

Not to be confused with prior implementations:

* [TerrariaFlagRandomizer](https://github.com/Cronus-waters/TerrariaFlagRandomizer)
* [TerrariaAchipelago](https://github.com/Whoneedspacee/TerrariaArchipelago)

This implementations uses boss kills and event clears as checks. The items are the permanent
changes to the world that bosses reward in vanilla. Things like "Witch Doctor can now spawn",
"The Dungeon is in Post-Plantera mode", and various things caused by Hardmode. Bosses still drop
their original loot, though.

See [Archipelago](https://archipelago.gg/) if you don't know what it is.

## Future Work

- [X] Remove effects from original bosses
- [X] Re-add effects conditionally
- [X] Server-side implementation
- [X] Announce boss / event clears to server
- [X] Client responses to server
- [ ] Archipelago features (deathlink, in-game commands, etc.)
- [ ] Integrate with other implementations

## Usage

### For the Terraria player

Clone/download this repo into Documents/My Games/Terraria/tModLoader/ModSources such that
SeldomArchipelago.sln is at
Documents/My Games/Terraria/tModLoader/ModSources/SeldomArchipelago/SeldomArchipelago.sln.
In tModLoader, go to Workshop > Develop Mods, and click "Build + Reload" under SeldomArchipelago.
In Workshop > Manage Mods, edit Archipelago Randomizer (Seldom's Implementation)'s settings.
Since this game isn't on Archipelago's main server, you'll need to set all of the settings,
including the advanced ones. If you're hosting, the address will probably be "localhost"
and the server should tell you the port. If not, whoever's hosting should know.

You may use as many worlds as you like, but joining pre-existing worlds or using pre-existing
characters may mess with it. If you want to cheat, disable the mod first, do your business,
and then re-enable it. It's also supposed to work in multiplayer, but I haven't tested that
as much.

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
