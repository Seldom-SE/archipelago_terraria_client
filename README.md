# Archipelago Terraria Client

Not to be confused with prior implementations:

- [TerrariaFlagRandomizer](https://github.com/Cronus-waters/TerrariaFlagRandomizer)
- [TerrariaArchipelago](https://github.com/Whoneedspacee/TerrariaArchipelago)

Archipelago is a multiworld randomizer, which is a sort of multi-game mod that shuffles things
between games. So, for example, when you kill Skeletron in your Terraria game, your friend might get
Mothwing Cloak in their Hollow Knight game. But you can't enter the Dungeon until your friend picks
up the item at Vengeful Spirit's location in their game. Archipelago has support for a lot of games.
Learn more on its [website](https://archipelago.gg/).

This implementation uses boss kills and event clears as checks. The items are the permanent changes
to the world that bosses and events reward in vanilla. Things like "Post-Plantera" and "Hardmode".
Bosses still drop their original loot, though. Optionally, you may also include achievement checks
and item rewards.

Because this mod adds an extra layer of multiplayer, there are many ways to use it:
- Randomize one singleplayer Terraria world
- Randomize one multiplayer Terraria world
- Randomize multiple singleplayer or multiplayer Terraria worlds between each other
- Randomize Terraria with entirely separate games

## Usage

### For the Terraria player

Subscribe to the mod at
[its Steam page](https://steamcommunity.com/sharedfiles/filedetails/?id=2922217554). In Workshop >
Manage Mods, edit Archipelago Randomizer (Seldom's Implementation)'s settings. If you're using
archipelago.gg to host, leave the server address as "archipelago.gg". If you're hosting, the address
will probably be "localhost" and the server should tell you the port. If not, whoever's hosting
should know.

You may use as many worlds as you like. You may open your world in multiplayer for others to join.
Use `/ap` to use the Archipelago console, and `/apflags` to check what boss and event flags you
have.

### For the host, if you have any Terraria players

You can host easily on [archipelago.gg](https://archipelago.gg/).

For shorter runs, if this mod has upcoming content, and if it's up, you can use
[Archipelago's beta site](http://archipelago.gg:24242/).

Or, if you know how to use them, you can run locally with Terraria's `.apworld`. Check the
`#terraria` channel on Archipelago's Discord for the latest `.apworld` if there is one.

Alternatively, if you have the technical knowledge, you can run from source. Run
[Archipelago](https://github.com/ArchipelagoMW/Archipelago) (or a branch on
[my fork](https://github.com/Seldom-SE/Archipelago)) from source (see this
[guide](https://github.com/ArchipelagoMW/Archipelago/blob/main/docs/running%20from%20source.md)).
You will need to run `Generate.py` and `MultiServer.py`. To create the `yaml` files, you can run
`WebHost.py`. This will run a website, so watch the console for an address including `localhost`,
and go there on your browser. Terraria players have to setup their `yaml` files from this website,
but other games should work fine from [the official site](https://archipelago.gg/). You may instead
host the game with `WebHost.py`, if you prefer.

## License

Archipelago Terraria Client is licensed under MIT. The Archipelago logo, by Krista Corkos and
Chistopher Wilson, is licensed under CC BY-NC 4.0. The icon and collection button image used by this
mod are modified versions of the Archipelago logo, made to fit Terraria's style.
