namespace lawar4.Services;

/// <summary>Verbatim prompt and JSON schema used for structured screenshot extraction.</summary>
public static class ExtractionPrompt
{
    public const string CacheVersion = "weekly-extractor-v4-day-confidence-player-line-2026-08-27";

    public const string Prompt = """

You are extracting ranking data from a mobile game screenshot.

Return only data matching the supplied JSON schema.

TASK
1. Determine which weekday tab is selected.
2. Normalize the selected day to exactly one of:
   monday, tuesday, wednesday, thursday, friday, saturday.
3. Do NOT rely only on the written language of the tab.
   The UI language may be English, Turkish, Arabic, or mixed.
   Prefer the VISUAL POSITION of the selected tab:
   1=Monday, 2=Tuesday, 3=Wednesday,
   4=Thursday, 5=Friday, 6=Saturday.
   IMPORTANT: day_confidence is a confidence/probability from 0.0 to 1.0.
   It is NOT the weekday position/number. For example, for Saturday return
   detected_day="saturday" and day_confidence=1.0 (or another value <= 1.0), NEVER 6.
4. Extract every sufficiently visible ranking row in the main scrolling list.
5. Each leaderboard entry has TWO text lines beside the avatar:
   - FIRST / UPPER line: the PLAYER NAME (and, on some screenshots, a numeric player ID before it).
   - SECOND / LOWER line: the ALLIANCE / ROLE text, e.g. "[EfC] Elite Force Commander".
   These are different fields. NEVER return the second/lower alliance line as raw_name.
   For each row extract:
   - rank
   - player ID only if it is explicitly visible before/near the player name on the FIRST line
   - raw_name: ONLY the player name from the FIRST / UPPER line, exactly as visible
   - alliance_name: ONLY the SECOND / LOWER alliance or role line, exactly as visible; null if absent/unreadable
   - points as an integer with separators removed
   - avatar_bbox: the tight bounding box of the player's square/circular avatar,
     using normalized screenshot coordinates from 0 to 1000 for x, y, width, height.
     Return null only when the avatar is not sufficiently visible.
6. A highlighted/pinned alliance/self row may appear separately at the bottom.
   Extract it into pinned_row and DO NOT include it again in rows.
7. Never infer a missing player ID from rank, avatar, name, or prior knowledge.
8. Preserve Unicode characters in player names.
9. Do not translate player names.
10. Do not confuse alliance/role text with the player name. Capture the second line in alliance_name,
    but NEVER copy it into raw_name.
11. Ignore banners, timers, announcements, buttons, headers, and unrelated UI text.
12. If a row is too obscured to read its score reliably, omit it rather than inventing data.
13. If text is ambiguous:
    - return the best visible reading,
    - lower extraction_confidence,
    - add a short warning.
14. Rank means leaderboard position, not player ID.
15. UI language is descriptive only and may be:
    english, turkish, arabic, mixed, unknown.

Important quality rules:
- Copy all digits carefully.
- Distinguish player ID from leaderboard rank.
- Do not "correct" unusual spellings.
- Do not fabricate IDs for screenshots that do not display IDs.
- raw_name must come from the FIRST / UPPER text line next to the avatar.
- alliance_name must come from the SECOND / LOWER text line next to the avatar.
- If the visible text is "Player123" on the first line and "[EfC] Elite Force Commander" on the second,
  raw_name MUST be "Player123" and alliance_name MUST be "[EfC] Elite Force Commander".
- day_confidence MUST be between 0.0 and 1.0 and MUST NEVER contain the weekday index (1-6).
- avatar_bbox must contain only the avatar/profile image and its decorative frame, not the rank or name.
- Coordinates are normalized to the ENTIRE supplied screenshot: top-left=(0,0), bottom-right=(1000,1000).

""";

    public const string SchemaName = "ranking_screenshot_extraction";

    public const string SchemaJson = """
{
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "detected_day": { "type": "string", "enum": ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday"] },
    "day_confidence": { "type": "number", "minimum": 0, "maximum": 6 },
    "ui_language": { "type": "string", "enum": ["english", "turkish", "arabic", "mixed", "unknown"] },
    "rows": {
      "type": "array",
      "items": {
        "type": "object",
        "additionalProperties": false,
        "properties": {
          "rank": { "type": "integer", "minimum": 1 },
          "player_id": { "anyOf": [{ "type": "integer", "minimum": 1 }, { "type": "null" }] },
          "raw_name": { "type": "string", "minLength": 1 },
          "alliance_name": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
          "points": { "type": "integer", "minimum": 0 },
          "extraction_confidence": { "type": "number", "minimum": 0, "maximum": 1 },
          "avatar_bbox": {
            "anyOf": [
              { "type": "null" },
              {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "x": { "type": "integer", "minimum": 0, "maximum": 1000 },
                  "y": { "type": "integer", "minimum": 0, "maximum": 1000 },
                  "width": { "type": "integer", "minimum": 1, "maximum": 1000 },
                  "height": { "type": "integer", "minimum": 1, "maximum": 1000 }
                },
                "required": ["x", "y", "width", "height"]
              }
            ]
          }
        },
        "required": ["rank", "player_id", "raw_name", "alliance_name", "points", "extraction_confidence", "avatar_bbox"]
      }
    },
    "pinned_row": {
      "anyOf": [
        { "type": "null" },
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "rank": { "type": "integer", "minimum": 1 },
            "player_id": { "anyOf": [{ "type": "integer", "minimum": 1 }, { "type": "null" }] },
            "raw_name": { "type": "string", "minLength": 1 },
            "alliance_name": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
            "points": { "type": "integer", "minimum": 0 },
            "extraction_confidence": { "type": "number", "minimum": 0, "maximum": 1 },
            "avatar_bbox": {
              "anyOf": [
                { "type": "null" },
                {
                  "type": "object",
                  "additionalProperties": false,
                  "properties": {
                    "x": { "type": "integer", "minimum": 0, "maximum": 1000 },
                    "y": { "type": "integer", "minimum": 0, "maximum": 1000 },
                    "width": { "type": "integer", "minimum": 1, "maximum": 1000 },
                    "height": { "type": "integer", "minimum": 1, "maximum": 1000 }
                  },
                  "required": ["x", "y", "width", "height"]
                }
              ]
            }
          },
          "required": ["rank", "player_id", "raw_name", "alliance_name", "points", "extraction_confidence", "avatar_bbox"]
        }
      ]
    },
    "warnings": { "type": "array", "items": { "type": "string" } }
  },
  "required": ["detected_day", "day_confidence", "ui_language", "rows", "pinned_row", "warnings"]
}
""";

    public const string WarCacheVersion = "war-extractor-v1-desert-canyon-2026-09-27";

    public const string WarPrompt = """

You are extracting ranking and battle score data from a mobile game screenshot of Desert Storm or Canyon Storm war battle results.

Return only data matching the supplied JSON schema.

TASK
1. Determine the event type from the header / banner:
   - "desert_storm" if the title or banner mentions Desert Storm / Desert Battle.
   - "canyon_storm" if the title or banner mentions Canyon Storm / Canyon Storm Battlefield / Task Force.
   - "unknown" if neither is identifiable.
2. Set detected_day to the normalized event type ("desert_storm", "canyon_storm", or "unknown").
3. Set day_confidence to a confidence/probability from 0.0 to 1.0 indicating your confidence in the detected event.
4. Extract every sufficiently visible ranking row in the battle results list.
5. Each battle results row contains:
   - RANK: on the left (medal icon with 1, 2, or 3, or numeric text like 4, 5, 10, 35, 54, etc.).
   - AVATAR: square or framed player avatar image.
   - UPPER TEXT LINE: the player name (e.g. "[EfC]OGRAGNAR", "[EfC]T a z ツ", "[EfC]veammar"). Extract this verbatim into raw_name.
     If an alliance tag like "[EfC]" is present in this line, keep it in raw_name.
     If a numeric player ID is explicitly visible on this line before/near the name, extract it into player_id (otherwise player_id is null).
   - LOWER TEXT LINE: the player's battle score / points (e.g. 6438708, 2857430, 100520). Extract this integer into points.
   - alliance_name: null (or alliance tag if distinct from the player name).
   - avatar_bbox: the tight bounding box of the player's square/circular avatar, using normalized screenshot coordinates from 0 to 1000 for x, y, width, height.
6. A highlighted/pinned self row may appear separately at the bottom.
   If present, extract it into pinned_row and DO NOT include it again in rows.
7. Preserve Unicode characters and emojis/symbols in player names (e.g. ツ, 亗, ™, 乂, σ, etc.).
8. Do not translate or alter player names or unusual spellings.
9. Ignore banners, mail headers, delete/star buttons, back buttons, and unrelated UI decorations.
10. If a row is too obscured to read its score reliably, omit it rather than inventing data.
11. If text is ambiguous:
    - return the best visible reading,
    - lower extraction_confidence,
    - add a short warning.
12. Rank means leaderboard position, not player ID.
13. UI language is descriptive only and may be:
    english, turkish, arabic, mixed, unknown.

Important quality rules:
- Copy all digits carefully for scores and ranks.
- raw_name must come from the UPPER text line next to the avatar.
- points must come from the LOWER text line containing the numeric score.
- avatar_bbox must contain only the avatar image, not the rank or name.
- Coordinates are normalized to the ENTIRE supplied screenshot: top-left=(0,0), bottom-right=(1000,1000).

""";

    public const string WarSchemaName = "war_screenshot_extraction";

    public const string WarSchemaJson = """
{
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "detected_day": { "type": "string", "enum": ["desert_storm", "canyon_storm", "unknown"] },
    "day_confidence": { "type": "number", "minimum": 0, "maximum": 1 },
    "ui_language": { "type": "string", "enum": ["english", "turkish", "arabic", "mixed", "unknown"] },
    "rows": {
      "type": "array",
      "items": {
        "type": "object",
        "additionalProperties": false,
        "properties": {
          "rank": { "type": "integer", "minimum": 1 },
          "player_id": { "anyOf": [{ "type": "integer", "minimum": 1 }, { "type": "null" }] },
          "raw_name": { "type": "string", "minLength": 1 },
          "alliance_name": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
          "points": { "type": "integer", "minimum": 0 },
          "extraction_confidence": { "type": "number", "minimum": 0, "maximum": 1 },
          "avatar_bbox": {
            "anyOf": [
              { "type": "null" },
              {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                  "x": { "type": "integer", "minimum": 0, "maximum": 1000 },
                  "y": { "type": "integer", "minimum": 0, "maximum": 1000 },
                  "width": { "type": "integer", "minimum": 1, "maximum": 1000 },
                  "height": { "type": "integer", "minimum": 1, "maximum": 1000 }
                },
                "required": ["x", "y", "width", "height"]
              }
            ]
          }
        },
        "required": ["rank", "player_id", "raw_name", "alliance_name", "points", "extraction_confidence", "avatar_bbox"]
      }
    },
    "pinned_row": {
      "anyOf": [
        { "type": "null" },
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "rank": { "type": "integer", "minimum": 1 },
            "player_id": { "anyOf": [{ "type": "integer", "minimum": 1 }, { "type": "null" }] },
            "raw_name": { "type": "string", "minLength": 1 },
            "alliance_name": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
            "points": { "type": "integer", "minimum": 0 },
            "extraction_confidence": { "type": "number", "minimum": 0, "maximum": 1 },
            "avatar_bbox": {
              "anyOf": [
                { "type": "null" },
                {
                  "type": "object",
                  "additionalProperties": false,
                  "properties": {
                    "x": { "type": "integer", "minimum": 0, "maximum": 1000 },
                    "y": { "type": "integer", "minimum": 0, "maximum": 1000 },
                    "width": { "type": "integer", "minimum": 1, "maximum": 1000 },
                    "height": { "type": "integer", "minimum": 1, "maximum": 1000 }
                  },
                  "required": ["x", "y", "width", "height"]
                }
              ]
            }
          },
          "required": ["rank", "player_id", "raw_name", "alliance_name", "points", "extraction_confidence", "avatar_bbox"]
        }
      ]
    },
    "warnings": { "type": "array", "items": { "type": "string" } }
  },
  "required": ["detected_day", "day_confidence", "ui_language", "rows", "pinned_row", "warnings"]
}
""";
}
