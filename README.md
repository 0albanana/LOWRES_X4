# LOWRES_X4

A tool to reduce X4 Foundation's VRAM usage (space sim game by Egosoft).

https://forum.egosoft.com/viewtopic.php?f=146&t=409184

FAQ:
* Using the texture-modifying functionality will set the game to the "modified" state.
  * Currently there doesn't seem to be a way around that:
    X_sig.cat/.dat seem to contain hashes of files in X.cat/.dat, which are signed using
    a secret private key by Egosoft.
* Doesn't stick to savegames though, so the game can be safely reverted back to vanilla without loosing any progress.
* The app does not "remember" any changes it made to game files, so applying the same settings twice will lead to a really bad
experience ;)
