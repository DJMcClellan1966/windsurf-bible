{
  "Word": "covenant",
  "OriginalLanguage": "Hebrew",
  "Transliteration": "berith",
  "StrongsNumber": "H1285",
  "Definition": "A solemn agreement or treaty",
  "AlternateMeanings": ["binding obligation", "alliance", "pledge"],
  "CulturalContext": "Ancient Near Eastern covenants involved ritual ceremonies (e.g., cutting animals in half). When God made covenant with Abraham, He alone passed through the pieces, taking all responsibility.",
  "ExampleVerses": ["Genesis 15:18", "Exodus 24:7-8", "Jeremiah 31:31"]
}
# Adding Historical & Cultural Context Data

## Quick Start

The system now automatically creates initial knowledge base data with:
- **10 historical contexts** (Egyptian slavery, Roman occupation, Pharisees/Sadducees, etc.)
- **5 language insights** (shalom, agape, metanoia, pistis, charis)
- **6 thematic connections** (leadership failure, wilderness testing, etc.)

Data is stored in JSON files at:
```
%LOCALAPPDATA%\AIBibleApp\KnowledgeBase\
  ├── historical_context.json
  ├── language_insights.json
  └── thematic_connections.json
```

## How to Add More Data

### Option 1: Edit JSON Files Directly

Navigate to the KnowledgeBase folder and edit the JSON files:

**Add Historical Context:**
```json
{
  "Title": "Tax Collectors in Roman Judea",
  "Period": "Roman Occupation",
  "Category": "Social Structure",
  "Content": "Tax collectors (publicans) were Jews who collected taxes for Rome, taking a percentage. They were hated as traitors and thieves. Jesus eating with Matthew (a tax collector) was scandalous - rabbis avoided even their shadow. This makes Jesus' choice of Matthew as disciple revolutionary.",
  "RelatedCharacters": ["jesus", "matthew", "peter"],
  "Keywords": ["tax", "traitor", "sinner", "matthew", "zacchaeus"],
  "Source": "Roman taxation records, Josephus",
  "RelevanceWeight": 8
}
```

**Add Language Insight:**
```json
{
  "Word": "righteousness",
  "OriginalLanguage": "Greek",
  "Transliteration": "dikaiosyne",
  "StrongsNumber": "G1343",
  "Definition": "Right standing with God; justice, equity, rightness of life and conduct",
  "AlternateMeanings": ["justice", "justification", "rightness", "equity"],
  "CulturalContext": "In Greek thought, dikaiosyne meant fair dealing and civic virtue. In biblical use, it's about being in right relationship with God - not moral perfection but covenant faithfulness. Abraham's faith was 'counted as righteousness' (Genesis 15:6) - meaning God declared him in right standing.",
  "ExampleVerses": ["Genesis 15:6", "Romans 3:21-22", "Matthew 5:6", "2 Corinthians 5:21"]
}
```

**Add Thematic Connection:**
```json
{
  "Theme": "Doubt Leading to Faith",
  "PrimaryPassage": "Genesis 18:9-15",
  "SecondaryPassage": "Luke 1:26-38",
  "ConnectionType": "Contrast",
  "Insight": "Sarah laughed in doubt when told she'd have a child in old age. Mary said 'let it be' in faith when told she'd have a child as a virgin. Both faced impossible situations, but Mary's response shows how faith has matured through Israel's history with God.",
  "RelatedCharacters": ["mary", "jesus"]
}
```

### Option 2: Programmatically Add Data

Create a data import service:

```csharp
public class KnowledgeBaseImporter
{
    private readonly IKnowledgeBaseService _knowledgeBase;
    
    public async Task ImportFromCsvAsync(string csvPath)
    {
        // Read CSV file with historical contexts
        // Parse and add to knowledge base
    }
    
    public async Task ImportStrongsDataAsync(string strongsPath)
    {
        // Import Strong's Concordance data
        // Create LanguageInsight entries
    }
}
```

### Option 3: Use External Sources

**For Historical Context:**
1. **Bible Encyclopedias** (public domain):
   - International Standard Bible Encyclopedia (ISBE)
   - Easton's Bible Dictionary
   - Smith's Bible Dictionary

2. **Archaeological Sources**:
   - Biblical Archaeology Review summaries
   - Museum catalogs (British Museum, Israel Museum)

3. **Academic Papers** (open access):
   - JSTOR public domain articles
   - Academia.edu papers

**For Language Insights:**
1. **Strong's Concordance** (public domain)
2. **Blue Letter Bible** (API available)
3. **Hebrew/Greek lexicons** (public domain)

**Format for Import:**
Create a CSV with columns:
```
Type,Title,Period,Category,Content,Characters,Keywords,Source,Weight
Historical,"Crucifixion","Roman",Politics,"Description...",jesus;peter,cross;death,Josephus,10
```

## Data Guidelines

### Historical Context
- **Title**: Short, descriptive (50 chars max)
- **Period**: Use consistent period names (Egyptian Bondage, United Monarchy, Exile, Roman Occupation, etc.)
- **Category**: Politics, Religion, Culture, Social Structure, Daily Life, Economy
- **Content**: 2-4 paragraphs, written conversationally
- **RelatedCharacters**: Use character IDs from your system (moses, david, paul, etc.)
- **Keywords**: 5-10 words for semantic matching
- **RelevanceWeight**: 1-10 (how essential this context is)

### Language Insights
- **Word**: English word as it appears in translations
- **OriginalLanguage**: "Hebrew", "Greek", or "Aramaic"
- **Transliteration**: How to pronounce the original word
- **StrongsNumber**: Format as "H####" or "G####"
- **Definition**: Single sentence core meaning
- **AlternateMeanings**: Other valid translations
- **CulturalContext**: 1-2 paragraphs explaining significance
- **ExampleVerses**: 3-5 key verses using this word

### Thematic Connections
- **Theme**: The big idea connecting these passages
- **ConnectionType**: "Parallel", "Contrast", "Fulfillment", "Echo", "Typology"
- **Insight**: Why this connection matters (2-3 sentences)
- **RelatedCharacters**: Characters who would reference these passages

## Integration

The knowledge base is automatically integrated into conversations:

1. **Automatic Context Injection**: 
   - When user asks Moses about leadership → Egyptian slavery context injected
   - When user quotes John 3:16 → Agape language insight injected

2. **Character-Specific**:
   - Moses gets Egyptian period context
   - Paul gets Roman period context
   - David gets Kingdom period context

3. **Question-Triggered**:
   - Keywords in user question match context keywords
   - Most relevant contexts (top 2-3) added to AI prompt

## Examples of Good Context

**✅ Good**: 
"Shepherds were considered lowly and unclean by urban Israelites - they lived outdoors, couldn't observe ceremonial purity, smelled of sheep. Yet David was a shepherd before king. This makes 'The Lord is my shepherd' profound - God takes the lowly role."

**❌ Too academic**: 
"The socioeconomic stratification of Ancient Near Eastern pastoral occupations demonstrates a hierarchical valuation wherein..."

**✅ Good**: 
"Pharaoh claimed to be a god. The plagues mocked Egyptian gods - Nile to blood (Hapi), darkness (Ra), death of firstborn (Pharaoh's divinity). It was a battle between gods."

**❌ Too brief**: 
"Plagues targeted Egyptian gods."

## Future Enhancements

### Phase 1 (Now)
- ✅ Manual JSON editing
- ✅ 10 historical contexts
- ✅ 5 language insights
- ✅ 6 thematic connections

### Phase 2 (Next)
- CSV import tool
- Strong's Concordance integration
- 50+ historical contexts
- 25+ language insights

### Phase 3 (Later)
- Web scraping from public domain sources
- Automatic context generation from scholarly articles
- User-contributed contexts (curated)
- Multi-language support

## Testing Your Data

After adding new contexts:

1. **Test character conversation**:
   - Ask Moses about slavery → Should reference Egyptian slavery system
   - Ask Paul about grace → Should reference Greek "charis" meaning

2. **Check logs**:
   - Look for "Added {Count} historical contexts" in debug logs
   - Verify correct contexts are being selected

3. **Validate responses**:
   - Do characters sound more authentic?
   - Do they reference historical/cultural details?
   - Are word meanings explained when relevant?

## Pro Tips

1. **Quality over quantity**: 10 excellent contexts > 100 mediocre ones
2. **Write conversationally**: Characters will quote this directly
3. **Citation matters**: Always include source for credibility
4. **Keywords are key**: These drive relevance matching
5. **Test with real questions**: Use actual user questions to verify
6. **Update iteratively**: Start small, refine based on usage

## Resources

- [International Standard Bible Encyclopedia (ISBE)](https://www.biblestudytools.com/encyclopedias/isbe/)
- [Strong's Concordance](https://www.blueletterbible.org/lexicon/)
- [Biblical Archaeology Society](https://www.biblicalarchaeology.org/)
- [Ancient History Encyclopedia](https://www.worldhistory.org/)

---

The system is ready to use! Start a conversation and see historical context in action. As you identify gaps, add more contexts to the JSON files.
