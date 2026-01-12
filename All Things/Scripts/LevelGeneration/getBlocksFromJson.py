
# load list.txt file with list of blocks in every line and get only name part seperated with ;
block_names = []
with open("Assets/All Things/Scripts/LevelGeneration/_list.txt", "r") as f:
    for line in f:
        # remove .png in last
        block_name = line.split(";")[0].strip()
        if block_name.endswith(".png"):
            block_name = block_name[:-4]
        block_names.append(block_name)

# Print comma-separated
for i in range(0, len(block_names), 20):
    print(", ".join(block_names[i:i + 20]) + ",")
    