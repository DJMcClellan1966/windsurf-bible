import re

test_files = ["1CH01.htm", "GEN01.htm", "JHN03.htm", "2CO13.htm", "PSA023.htm", "PSA001.htm"]

for filename in test_files:
    # Remove .htm extension first
    name_no_ext = filename.replace('.htm', '')
    # Match letters/numbers at start, then extract trailing digits as chapter
    match = re.match(r'([A-Z0-9]+?)(\d+)$', name_no_ext)
    if match:
        book_code = match.group(1)
        chapter = match.group(2)
        print(f"{filename} -> Book: {book_code}, Chapter: {chapter}")
    else:
        print(f"{filename} -> No match")
