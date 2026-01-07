import json

# Load JSON file (index)
with open("Assets/All Things/Scripts/LevelGeneration/_list.json", "r", encoding="utf-8") as f:
    data = json.load(f)

# Get block names without .json
block_names = [
    filename.replace(".json", "")
    for filename in data.get("files", [])
    if filename.endswith(".json")
]

# Print comma-separated
for i in range(0, len(block_names), 20):
    print(", ".join(block_names[i:i + 20]) + ",")
    