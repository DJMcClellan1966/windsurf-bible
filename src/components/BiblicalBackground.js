import React, { useState } from 'react';
import './BiblicalBackground.css';

const BiblicalBackground = () => {
  const [activeTab, setActiveTab] = useState('words');
  const [searchTerm, setSearchTerm] = useState('');

  // Sample data - can be expanded with actual database
  const wordStudies = [
    {
      word: 'Agape',
      language: 'Greek',
      transliteration: 'ἀγάπη',
      meaning: 'Unconditional love',
      usage: 'Used 116 times in the New Testament',
      context: 'Refers to the highest form of love, particularly God\'s love for humanity',
      references: ['John 3:16', '1 Corinthians 13', '1 John 4:8']
    },
    {
      word: 'Shalom',
      language: 'Hebrew',
      transliteration: 'שָׁלוֹם',
      meaning: 'Peace, completeness, welfare',
      usage: 'Appears over 250 times in the Old Testament',
      context: 'More than absence of conflict; represents wholeness and harmony with God',
      references: ['Numbers 6:26', 'Psalm 29:11', 'Isaiah 26:3']
    },
    {
      word: 'Hesed',
      language: 'Hebrew',
      transliteration: 'חֶסֶד',
      meaning: 'Loving-kindness, steadfast love, mercy',
      usage: 'Found 248 times in the Old Testament',
      context: 'Describes God\'s covenantal loyalty and faithful love',
      references: ['Psalm 136', 'Lamentations 3:22-23', 'Hosea 6:6']
    },
    {
      word: 'Abba',
      language: 'Aramaic',
      transliteration: 'אַבָּא',
      meaning: 'Father (intimate)',
      usage: 'Used 3 times in the New Testament',
      context: 'An intimate term for father, like "Papa" or "Daddy"',
      references: ['Mark 14:36', 'Romans 8:15', 'Galatians 4:6']
    }
  ];

  const historicalPlaces = [
    {
      name: 'Jerusalem',
      significance: 'The Holy City, center of Jewish worship',
      history: 'Capital of ancient Israel under King David (c. 1000 BC)',
      archaeology: 'City of David excavations reveal structures from 10th century BC',
      biblicalEvents: ['Solomon\'s Temple', 'Jesus\' crucifixion and resurrection', 'Pentecost'],
      modernLocation: 'Capital of Israel',
      facts: ['Over 3000 years old', 'Sacred to Judaism, Christianity, and Islam', 'Destroyed and rebuilt multiple times']
    },
    {
      name: 'Bethlehem',
      significance: 'Birthplace of Jesus Christ',
      history: 'Ancient city mentioned in Genesis, home of Ruth and Jesse',
      archaeology: 'Church of the Nativity built in 326 AD over traditional birthplace',
      biblicalEvents: ['Birth of Jesus', 'Home of King David', 'Ruth and Boaz\'s story'],
      modernLocation: 'West Bank, Palestine',
      facts: ['Name means "House of Bread" in Hebrew', 'Only 6 miles from Jerusalem', 'Mentioned 44 times in the Bible']
    },
    {
      name: 'Capernaum',
      significance: 'Jesus\' base of ministry in Galilee',
      history: 'Fishing village on the Sea of Galilee',
      archaeology: 'Excavated synagogue from 4th century, built over 1st-century foundation',
      biblicalEvents: ['Jesus called Peter, Andrew, James, and John', 'Many miracles performed', 'Teaching in the synagogue'],
      modernLocation: 'Northern Israel, Sea of Galilee',
      facts: ['Called "Jesus\' own city" in Matthew 9:1', 'Home of Peter', 'Prosperous fishing town']
    },
    {
      name: 'Jericho',
      significance: 'One of the oldest continuously inhabited cities',
      history: 'Dating back to 9000 BC, conquered by Joshua',
      archaeology: 'Tel es-Sultan shows occupation layers dating to 8000 BC',
      biblicalEvents: ['Walls fell down for Joshua', 'Zacchaeus met Jesus', 'Good Samaritan parable location'],
      modernLocation: 'West Bank, Palestine',
      facts: ['Lowest city on Earth (850 feet below sea level)', 'Called "City of Palms"', 'Important trade route city']
    }
  ];

  const archaeologicalDiscoveries = [
    {
      name: 'Dead Sea Scrolls',
      date: '1947-1956',
      location: 'Qumran, near Dead Sea',
      significance: 'Oldest known biblical manuscripts',
      description: 'Over 900 manuscripts including every Old Testament book except Esther',
      impact: 'Confirmed accuracy of biblical transmission over 1000+ years',
      timeframe: '3rd century BC to 1st century AD'
    },
    {
      name: 'Pool of Siloam',
      date: '2004',
      location: 'Jerusalem',
      significance: 'Where Jesus healed the blind man (John 9)',
      description: 'Large ritual bath from Second Temple period',
      impact: 'Confirmed historical accuracy of John\'s Gospel',
      timeframe: '1st century AD'
    },
    {
      name: 'Pilate Stone',
      date: '1961',
      location: 'Caesarea Maritima',
      significance: 'Only archaeological evidence of Pontius Pilate',
      description: 'Limestone block with inscription mentioning Pilate as Prefect of Judaea',
      impact: 'Confirmed historical existence of Pontius Pilate',
      timeframe: '26-36 AD'
    },
    {
      name: 'Tel Dan Inscription',
      date: '1993-1994',
      location: 'Tel Dan, Northern Israel',
      significance: 'First historical evidence of King David',
      description: 'Stone fragment mentioning "House of David"',
      impact: 'Confirmed historical existence of King David\'s dynasty',
      timeframe: '9th century BC'
    }
  ];

  const biblicalPeople = [
    {
      name: 'Moses',
      role: 'Prophet and Lawgiver',
      era: 'c. 1391-1271 BC (traditional)',
      significance: 'Led Israelites out of Egypt, received Ten Commandments',
      interestingFacts: [
        'Name means "drawn out" in Hebrew',
        'Spent 40 years in Egypt, 40 in Midian, 40 leading Israel',
        'Only person to speak with God face to face',
        'Wrote first five books of the Bible (Torah)'
      ],
      references: ['Exodus', 'Numbers', 'Deuteronomy']
    },
    {
      name: 'King David',
      role: 'King of Israel',
      era: 'c. 1040-970 BC',
      significance: 'United the kingdom, ancestor of Jesus',
      interestingFacts: [
        'Killed Goliath as a shepherd boy',
        'Wrote approximately 75 Psalms',
        'Called "a man after God\'s own heart"',
        'Jerusalem called "City of David"'
      ],
      references: ['1 Samuel', '2 Samuel', '1 Kings', 'Psalms']
    },
    {
      name: 'Apostle Paul',
      role: 'Missionary and Apostle',
      era: 'c. 5-67 AD',
      significance: 'Spread Christianity to Gentiles, wrote much of New Testament',
      interestingFacts: [
        'Originally persecuted Christians as Saul',
        'Converted on road to Damascus',
        'Made three missionary journeys',
        'Wrote 13 epistles in the New Testament'
      ],
      references: ['Acts', 'Romans through Philemon']
    },
    {
      name: 'Mary (Mother of Jesus)',
      role: 'Mother of Jesus Christ',
      era: '1st century AD',
      significance: 'Virgin birth, mother of the Messiah',
      interestingFacts: [
        'Young teenager when Jesus was born (likely 13-15)',
        'Cousin of Elizabeth (John the Baptist\'s mother)',
        'Present at Jesus\' first miracle in Cana',
        'Witnessed the crucifixion'
      ],
      references: ['Matthew', 'Luke', 'John', 'Acts']
    }
  ];

  const renderContent = () => {
    switch(activeTab) {
      case 'words':
        return (
          <div className="content-section">
            <h2>Biblical Word Studies</h2>
            <input 
              type="text" 
              placeholder="Search words..." 
              className="search-input"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
            <div className="cards-container">
              {wordStudies.filter(w => 
                w.word.toLowerCase().includes(searchTerm.toLowerCase()) ||
                w.meaning.toLowerCase().includes(searchTerm.toLowerCase())
              ).map((study, idx) => (
                <div key={idx} className="info-card word-card">
                  <div className="card-header">
                    <h3>{study.word}</h3>
                    <span className={`language-badge ${study.language.toLowerCase()}`}>
                      {study.language}
                    </span>
                  </div>
                  <p className="transliteration">{study.transliteration}</p>
                  <p className="meaning"><strong>Meaning:</strong> {study.meaning}</p>
                  <p className="usage"><strong>Usage:</strong> {study.usage}</p>
                  <p className="context"><strong>Context:</strong> {study.context}</p>
                  <div className="references">
                    <strong>Key References:</strong>
                    <ul>
                      {study.references.map((ref, i) => <li key={i}>{ref}</li>)}
                    </ul>
                  </div>
                </div>
              ))}
            </div>
          </div>
        );
      
      case 'places':
        return (
          <div className="content-section">
            <h2>Historical Places</h2>
            <div className="cards-container">
              {historicalPlaces.map((place, idx) => (
                <div key={idx} className="info-card place-card">
                  <h3>{place.name}</h3>
                  <p><strong>Significance:</strong> {place.significance}</p>
                  <p><strong>History:</strong> {place.history}</p>
                  <p><strong>Archaeology:</strong> {place.archaeology}</p>
                  <p><strong>Modern Location:</strong> {place.modernLocation}</p>
                  <div className="biblical-events">
                    <strong>Biblical Events:</strong>
                    <ul>
                      {place.biblicalEvents.map((event, i) => <li key={i}>{event}</li>)}
                    </ul>
                  </div>
                  <div className="facts">
                    <strong>Interesting Facts:</strong>
                    <ul>
                      {place.facts.map((fact, i) => <li key={i}>{fact}</li>)}
                    </ul>
                  </div>
                </div>
              ))}
            </div>
          </div>
        );
      
      case 'archaeology':
        return (
          <div className="content-section">
            <h2>Archaeological Discoveries</h2>
            <div className="cards-container">
              {archaeologicalDiscoveries.map((discovery, idx) => (
                <div key={idx} className="info-card archaeology-card">
                  <h3>{discovery.name}</h3>
                  <p className="discovery-date">Discovered: {discovery.date}</p>
                  <p><strong>Location:</strong> {discovery.location}</p>
                  <p><strong>Time Period:</strong> {discovery.timeframe}</p>
                  <p><strong>Description:</strong> {discovery.description}</p>
                  <p><strong>Significance:</strong> {discovery.significance}</p>
                  <p className="impact"><strong>Impact:</strong> {discovery.impact}</p>
                </div>
              ))}
            </div>
          </div>
        );
      
      case 'people':
        return (
          <div className="content-section">
            <h2>Biblical People</h2>
            <div className="cards-container">
              {biblicalPeople.map((person, idx) => (
                <div key={idx} className="info-card people-card">
                  <h3>{person.name}</h3>
                  <p className="role">{person.role}</p>
                  <p><strong>Era:</strong> {person.era}</p>
                  <p><strong>Significance:</strong> {person.significance}</p>
                  <div className="interesting-facts">
                    <strong>Interesting Facts:</strong>
                    <ul>
                      {person.interestingFacts.map((fact, i) => <li key={i}>{fact}</li>)}
                    </ul>
                  </div>
                  <p><strong>Key References:</strong> {person.references.join(', ')}</p>
                </div>
              ))}
            </div>
          </div>
        );
      
      default:
        return null;
    }
  };

  return (
    <div className="biblical-background-container">
      <h1>Biblical Background & Resources</h1>
      
      <div className="tabs">
        <button 
          className={activeTab === 'words' ? 'active' : ''}
          onClick={() => setActiveTab('words')}
        >
          📖 Word Studies
        </button>
        <button 
          className={activeTab === 'places' ? 'active' : ''}
          onClick={() => setActiveTab('places')}
        >
          🏛️ Historical Places
        </button>
        <button 
          className={activeTab === 'archaeology' ? 'active' : ''}
          onClick={() => setActiveTab('archaeology')}
        >
          🔍 Archaeological Discoveries
        </button>
        <button 
          className={activeTab === 'people' ? 'active' : ''}
          onClick={() => setActiveTab('people')}
        >
          👥 Biblical People
        </button>
      </div>

      {renderContent()}
    </div>
  );
};

export default BiblicalBackground;