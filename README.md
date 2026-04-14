# SpinLists
**Mod for Spin Rhythm XD that adds "playlist" functionality to the game**

Playlists can be found in the side panel when choosing a chart. Filters are automatically cleared whenever viewing a playlist.

# Playlist file structure
By default, playlist data is stored in the game's AppData/LocalLow folder (usually where custom chart data is stored) in the created `SpinLists` sub-directory: `C:\Users\USERNAME\AppData\LocalLow\Super Spin Digital\Spin Rhythm XD\SpinLists`

Playlist JSON data and cover art are stored in the same directory. The cover art's filename has to match the JSON data's filename (JPG and PNG are valid types), or it will use a fallback image if a valid cover art image cannot be found or loaded.

```
SpinLists/
├── SamplePlaylist1.json
├── SamplePlaylist1.png
├── SamplePlaylist2.json
└── SamplePlaylist2.jpg
```

----

```json
{
  "entries": [
    {
      "fileReference": "spinshare_696d993e60623",
      "title": "NiTE CRUiSiNG",
      "artist": "Srav3R",
      "charter": "★MrCringe★ [Feat. T2864.NEFirefox for lyrics]"
    },
    {
      "fileReference": "spinshare_689569d655922",
      "title": "Breathe",
      "artist": "Bad Computer",
      "charter": "Steven of Astora & Crooky",
      "difficulty": 6
    }
  ],
  "name": "Sample Playlist 1 (for Testing)"
}
```

----

| Field         | Required | Description                                           |
|---------------|----------|-------------------------------------------------------|
| `entries`     | Yes      | All of the playlist's charts/entries *(can be empty)* |
| `name`        | Yes      | Name of the playlist                                  |
| `author`      | No       | Author of the playlist                                |
| `description` | No       | Description of the playlist                           |

----

The `entries` array consists of objects with the following fields:

| Field           | Required | Description                  |
|-----------------|----------|------------------------------|
| `fileReference` | Yes      | File reference for the chart |
| `title`         | No       | Title for the chart          |
| `artist`        | No       | Artist credit for the chart  |
| `charter`       | No       | Charter credit for the chart |
| `difficulty`    | No       | Suggested difficulty         |

`difficulty` is an integer that corresponds to the following difficulty levels:

| Value | Difficulty |
|-------|------------|
| `2`     | Easy       |
| `3`     | Normal     |
| `4`     | Hard       |
| `5`     | Expert     |
| `6`     | XD         |
| `7`     | RemiXD     |

> [!NOTE]
> `0`, `1`, and `255` are valid values internally for the `TrackData.DifficultyType` enum, but are not intended to be used by actual charts.

----

# Dependencies
- SpinCore
- SpinShareLib
- Newtonsoft.Json

----

> [!NOTE]
> This mod is still a work in progress! Not releasing binaries until I can knock some stuff out. Namely:
> - ~~Automatically downloading any missing custom charts from SpinShare after alerting the user somehow~~
> - Cleaner chart selection UI updating
> - Cleaner UI flow overall
> - ~~Refreshing all available playlists/loading new ones~~
> - ~~Adding (optional) fields for a suggested difficulty~~
>   - ~~(and also auto-selecting it)~~ 
> - ~~Adding (optional) metadata fields (easy)~~
>   - ~~Playlist author~~
>   - ~~Playlist description~~
>     - ...maybe tooltips on the cover image for this?
> - Mod update notifications
>   - (this will also be easy, just very low priority) 
> 
> (eventually, also):
> - URL field to allow remote playlist updating
> - Creating new playlists from in-game
> - Deleting playlists from in-game