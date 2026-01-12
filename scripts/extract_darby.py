#!/usr/bin/env python3
"""
Extract Darby Translation verses from HTML files and convert to JSON format.
"""

import os
import json
import re
from pathlib import Path

# Darby directory with HTML chapter files
DARBY_DIR = r"C:\Users\DJMcC\OneDrive\Desktop\bible-playground\darby"
# Output JSON file for the app
OUTPUT_JSON = r"C:\Users\DJMcC\OneDrive\Desktop\bible-playground\src\AI-Bible-App.Maui\Data\Bible\darby.json"

# Book name mappings (consistent with other translations)
BOOK_NAMES = {
    "GEN": "Genesis", "EXO": "Exodus", "LEV": "Leviticus", "NUM": "Numbers", "DEU": "Deuteronomy",
    "JOS": "Joshua", "JDG": "Judges", "RUT": "Ruth", "1SA": "1 Samuel", "2SA": "2 Samuel",
    "1KI": "1 Kings", "2KI": "2 Kings", "1CH": "1 Chronicles", "2CH": "2 Chronicles",
    "EZR": "Ezra", "NEH": "Nehemiah", "EST": "Esther", "JOB": "Job", "PSA": "Psalms",
    "PRO": "Proverbs", "ECC": "Ecclesiastes", "SNG": "Song of Solomon", "ISA": "Isaiah",
    "JER": "Jeremiah", "LAM": "Lamentations", "EZK": "Ezekiel", "DAN": "Daniel",
    "HOS": "Hosea", "JOL": "Joel", "AMO": "Amos", "OBA": "Obadiah", "JON": "Jonah",
    "MIC": "Micah", "NAM": "Nahum", "HAB": "Habakkuk", "ZEP": "Zephaniah", "HAG": "Haggai",
    "ZEC": "Zechariah", "MAL": "Malachi",
    "MAT": "Matthew", "MRK": "Mark", "LUK": "Luke", "JHN": "John", "ACT": "Acts",
    "ROM": "Romans", "1CO": "1 Corinthians", "2CO": "2 Corinthians", "GAL": "Galatians",
    "EPH": "Ephesians", "PHP": "Philippians", "COL": "Colossians", "1TH": "1 Thessalonians",
    "2TH": "2 Thessalonians", "1TI": "1 Timothy", "2TI": "2 Timothy", "TIT": "Titus",
    "PHM": "Philemon", "HEB": "Hebrews", "JAS": "James", "1PE": "1 Peter", "2PE": "2 Peter",
    "1JN": "1 John", "2JN": "2 John", "3JN": "3 John", "JUD": "Jude", "REV": "Revelation"
}

# Book order for numbering
BOOK_ORDER = [
    "Genesis", "Exodus", "Leviticus", "Numbers", "Deuteronomy",
    "Joshua", "Judges", "Ruth", "1 Samuel", "2 Samuel",
    "1 Kings", "2 Kings", "1 Chronicles", "2 Chronicles",
    "Ezra", "Nehemiah", "Esther", "Job", "Psalms",
    "Proverbs", "Ecclesiastes", "Song of Solomon", "Isaiah",
    "Jeremiah", "Lamentations", "Ezekiel", "Daniel",
    "Hosea", "Joel", "Amos", "Obadiah", "Jonah",
    "Micah", "Nahum", "Habakkuk", "Zephaniah", "Haggai",
    "Zechariah", "Malachi",
    "Matthew", "Mark", "Luke", "John", "Acts",
    "Romans", "1 Corinthians", "2 Corinthians", "Galatians",
    "Ephesians", "Philippians", "Colossians", "1 Thessalonians",
    "2 Thessalonians", "1 Timothy", "2 Timothy", "Titus",
    "Philemon", "Hebrews", "James", "1 Peter", "2 Peter",
    "1 John", "2 John", "3 John", "Jude", "Revelation"
]

def parse_filename(filename):
    """Extract book code and chapter number from filename like 'JHN03.htm'"""
    match = re.match(r'([A-Z0-9]{3})(\d+)\.htm', filename)
    if match:
        book_code = match.group(1)
        chapter = int(match.group(2))
        return book_code, chapter
    return None, None

def extract_verses_from_html(html_content):
    """Extract verses from Darby HTML content"""
    verses = []
    
    # Pattern to match: <span class="verse" id="V1">1&#160;</span>verse text
    # Darby has similar structure to YLT
    pattern = r'<span class="verse" id="V(\d+)">(\d+)&#160;</span>(.*?)(?=<span class="verse"|</div>)'
    
    matches = re.findall(pattern, html_content, re.DOTALL)
    
    for match in matches:
        verse_id = match[0]
        verse_num = match[1]
        verse_text = match[2]
        
        # Clean up the verse text
        # Remove HTML tags
        text = re.sub(r'<[^>]+>', ' ', verse_text)
        
        # Decode HTML entities
        text = text.replace('&#160;', ' ')
        text = text.replace('&nbsp;', ' ')
        text = text.replace('&lt;', '<')
        text = text.replace('&gt;', '>')
        text = text.replace('&amp;', '&')
        text = text.replace('&quot;', '"')
        text = text.replace('&apos;', "'")
        
        # Remove square brackets (supplied words in Darby)
        text = text.replace('[', '').replace(']', '')
        
        # Clean up whitespace
        text = re.sub(r'\s+', ' ', text).strip()
        
        if text:
            verses.append({
                'verse_number': int(verse_num),
                'text': text
            })
    
    return verses

def get_testament(book_name):
    """Determine if book is Old or New Testament"""
    ot_books = BOOK_ORDER[:39]  # First 39 books are OT
    return "Old" if book_name in ot_books else "New"

def main():
    print(f"Processing Darby Translation from: {DARBY_DIR}")
    
    # Get all HTML chapter files
    html_files = [f for f in os.listdir(DARBY_DIR) if re.match(r'[A-Z0-9]{3}\d+\.htm', f)]
    html_files.sort()
    
    print(f"Found {len(html_files)} chapter files in Darby Translation")
    
    # Test first file
    if html_files:
        test_file = html_files[0]
        print(f"Testing first file: {test_file}")
        with open(os.path.join(DARBY_DIR, test_file), 'r', encoding='utf-8') as f:
            test_content = f.read()
            test_verses = extract_verses_from_html(test_content)
            print(f"Test extraction resulted in {len(test_verses)} verses")
            if test_verses:
                print(f"Sample verse: {test_verses[0]['text'][:50]}...")
    
    all_verses = []
    
    for html_file in html_files:
        book_code, chapter = parse_filename(html_file)
        
        if not book_code or book_code not in BOOK_NAMES:
            continue
        
        book_name = BOOK_NAMES[book_code]
        testament = get_testament(book_name)
        book_number = BOOK_ORDER.index(book_name) if book_name in BOOK_ORDER else 0
        
        file_path = os.path.join(DARBY_DIR, html_file)
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                html_content = f.read()
                verses = extract_verses_from_html(html_content)
                
                for verse in verses:
                    verse_obj = {
                        "Book": book_name,
                        "Chapter": chapter,
                        "Verse": verse['verse_number'],
                        "Text": verse['text'],
                        "Translation": "Darby",
                        "Reference": f"{book_name} {chapter}:{verse['verse_number']}",
                        "FullText": f"{book_name} {chapter}:{verse['verse_number']}: {verse['text']}",
                        "Testament": testament,
                        "BookNumber": book_number
                    }
                    all_verses.append(verse_obj)
        
        except Exception as e:
            print(f"Error processing {html_file}: {e}")
            continue
        
        # Progress update every 5000 verses
        if len(all_verses) % 5000 == 0 and len(all_verses) > 0:
            print(f"Processed {len(all_verses)} verses so far...")
    
    print(f"Total verses extracted: {len(all_verses)}")
    
    # Write to JSON file
    os.makedirs(os.path.dirname(OUTPUT_JSON), exist_ok=True)
    with open(OUTPUT_JSON, 'w', encoding='utf-8') as f:
        json.dump(all_verses, f, indent=2, ensure_ascii=False)
    
    print(f"Output written to: {OUTPUT_JSON}")
    print("Darby Translation has been successfully extracted!")

if __name__ == "__main__":
    main()
