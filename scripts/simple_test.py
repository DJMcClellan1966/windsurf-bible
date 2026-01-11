"""Simple test of one file."""
import re
import json

def extract_verses_from_html(html_content):
    """Extract verses using regex from HTML content."""
    verses = []
    
    # Find all verse markers and extract text between them
    pattern = r'id="V(\d+)">(\d+)&#160;</span>(.*?)(?=<span class="verse"|</div>)'
    
    matches = re.findall(pattern, html_content, re.DOTALL)
    print(f"Found {len(matches)} regex matches")
    
    for verse_num_str, display_num, text in matches:
        verse_num = int(verse_num_str)
        
        # Clean up the text
        # Remove HTML tags
        text = re.sub(r'<[^>]+>', ' ', text)
        # Decode HTML entities
        text = text.replace('&#160;', ' ')
        text = text.replace('&nbsp;', ' ')
        # Remove footnote markers
        text = re.sub(r'[†‡§¶]', '', text)
        # Clean up whitespace
        text = re.sub(r'\s+', ' ', text).strip()
        
        if text:
            verses.append({
                "Book": "Genesis",
                "Chapter": 1,
                "Verse": verse_num,
                "Text": text,
                "Translation": "WEB",
                "Reference": f"Genesis 1:{verse_num}",
                "FullText": f"Genesis 1:{verse_num}: {text}",
                "Testament": "Old",
                "BookNumber": 0
            })
    
    return verses

# Test on Genesis 1
filepath = r"C:\Users\DJMcC\OneDrive\Desktop\bible-playground\bible-playground\bible\GEN01.htm"
with open(filepath, 'r', encoding='utf-8') as f:
    html_content = f.read()

verses = extract_verses_from_html(html_content)
print(f"Extracted {len(verses)} verses")

if verses:
    print(f"\nFirst verse:")
    print(json.dumps(verses[0], indent=2))
    print(f"\nLast verse:")
    print(json.dumps(verses[-1], indent=2))
