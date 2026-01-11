"""Test extraction on a single file."""
import re

with open(r"C:\Users\DJMcC\OneDrive\Desktop\bible-playground\bible-playground\bible\JHN03.htm", 'r', encoding='utf-8') as f:
    html = f.read()

# Try different patterns
print("Testing pattern 1:")
pattern1 = r'<span class="verse" id="V(\d+)">(\d+)&#160;</span>(.*?)(?=<span class="verse" id="V\d+">|</div>)'
matches1 = re.findall(pattern1, html, re.DOTALL)
print(f"Found {len(matches1)} matches")
if matches1:
    print(f"First match: {matches1[0][:200]}")

# Try simpler pattern
print("\nTesting pattern 2:")
pattern2 = r'id="V(\d+)">(\d+)&#160;</span>(.*?)(?=<span class="verse"|</div>)'
matches2 = re.findall(pattern2, html, re.DOTALL)
print(f"Found {len(matches2)} matches")
if matches2:
    for i, (num, disp, text) in enumerate(matches2[:3]):
        clean_text = re.sub(r'<[^>]+>', '', text)
        clean_text = clean_text.replace('&#160;', ' ')
        clean_text = re.sub(r'\s+', ' ', clean_text).strip()
        print(f"Verse {num}: {clean_text[:100]}...")
