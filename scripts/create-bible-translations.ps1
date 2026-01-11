# Download ASV and YLT Bible translations
# These are public domain translations

$dataPath = "C:\Users\DJMcC\OneDrive\Desktop\bible-playground\bible-playground\Data\Bible"

Write-Host "Creating ASV and YLT Bible data files..." -ForegroundColor Cyan

# ASV - American Standard Version (1901)
# Sample verses in the same format as WEB
$asvVerses = @(
    @{ Book = "Genesis"; Chapter = 1; Verse = 1; Text = "In the beginning God created the heavens and the earth."; Translation = "ASV"; Testament = "OT"; BookNumber = 1 }
    @{ Book = "Genesis"; Chapter = 1; Verse = 2; Text = "And the earth was waste and void; and darkness was upon the face of the deep: and the Spirit of God moved upon the face of the waters."; Translation = "ASV"; Testament = "OT"; BookNumber = 1 }
    @{ Book = "Genesis"; Chapter = 1; Verse = 3; Text = "And God said, Let there be light: and there was light."; Translation = "ASV"; Testament = "OT"; BookNumber = 1 }
    @{ Book = "Psalms"; Chapter = 23; Verse = 1; Text = "Jehovah is my shepherd; I shall not want."; Translation = "ASV"; Testament = "OT"; BookNumber = 19 }
    @{ Book = "Psalms"; Chapter = 23; Verse = 2; Text = "He maketh me to lie down in green pastures; He leadeth me beside still waters."; Translation = "ASV"; Testament = "OT"; BookNumber = 19 }
    @{ Book = "Psalms"; Chapter = 23; Verse = 3; Text = "He restoreth my soul: He guideth me in the paths of righteousness for his name's sake."; Translation = "ASV"; Testament = "OT"; BookNumber = 19 }
    @{ Book = "Psalms"; Chapter = 23; Verse = 4; Text = "Yea, though I walk through the valley of the shadow of death, I will fear no evil; for thou art with me; Thy rod and thy staff, they comfort me."; Translation = "ASV"; Testament = "OT"; BookNumber = 19 }
    @{ Book = "Psalms"; Chapter = 23; Verse = 5; Text = "Thou preparest a table before me in the presence of mine enemies: Thou hast anointed my head with oil; My cup runneth over."; Translation = "ASV"; Testament = "OT"; BookNumber = 19 }
    @{ Book = "Psalms"; Chapter = 23; Verse = 6; Text = "Surely goodness and lovingkindness shall follow me all the days of my life; And I shall dwell in the house of Jehovah for ever."; Translation = "ASV"; Testament = "OT"; BookNumber = 19 }
    @{ Book = "Proverbs"; Chapter = 3; Verse = 5; Text = "Trust in Jehovah with all thy heart, And lean not upon thine own understanding:"; Translation = "ASV"; Testament = "OT"; BookNumber = 20 }
    @{ Book = "Proverbs"; Chapter = 3; Verse = 6; Text = "In all thy ways acknowledge him, And he will direct thy paths."; Translation = "ASV"; Testament = "OT"; BookNumber = 20 }
    @{ Book = "Isaiah"; Chapter = 40; Verse = 31; Text = "but they that wait for Jehovah shall renew their strength; they shall mount up with wings as eagles; they shall run, and not be weary; they shall walk, and not faint."; Translation = "ASV"; Testament = "OT"; BookNumber = 23 }
    @{ Book = "Jeremiah"; Chapter = 29; Verse = 11; Text = "For I know the thoughts that I think toward you, saith Jehovah, thoughts of peace, and not of evil, to give you hope in your latter end."; Translation = "ASV"; Testament = "OT"; BookNumber = 24 }
    @{ Book = "Matthew"; Chapter = 5; Verse = 3; Text = "Blessed are the poor in spirit: for theirs is the kingdom of heaven."; Translation = "ASV"; Testament = "NT"; BookNumber = 40 }
    @{ Book = "Matthew"; Chapter = 5; Verse = 4; Text = "Blessed are they that mourn: for they shall be comforted."; Translation = "ASV"; Testament = "NT"; BookNumber = 40 }
    @{ Book = "Matthew"; Chapter = 5; Verse = 5; Text = "Blessed are the meek: for they shall inherit the earth."; Translation = "ASV"; Testament = "NT"; BookNumber = 40 }
    @{ Book = "Matthew"; Chapter = 6; Verse = 33; Text = "But seek ye first his kingdom, and his righteousness; and all these things shall be added unto you."; Translation = "ASV"; Testament = "NT"; BookNumber = 40 }
    @{ Book = "Matthew"; Chapter = 11; Verse = 28; Text = "Come unto me, all ye that labor and are heavy laden, and I will give you rest."; Translation = "ASV"; Testament = "NT"; BookNumber = 40 }
    @{ Book = "Matthew"; Chapter = 11; Verse = 29; Text = "Take my yoke upon you, and learn of me; for I am meek and lowly in heart: and ye shall find rest unto your souls."; Translation = "ASV"; Testament = "NT"; BookNumber = 40 }
    @{ Book = "Matthew"; Chapter = 28; Verse = 19; Text = "Go ye therefore, and make disciples of all the nations, baptizing them into the name of the Father and of the Son and of the Holy Spirit:"; Translation = "ASV"; Testament = "NT"; BookNumber = 40 }
    @{ Book = "Matthew"; Chapter = 28; Verse = 20; Text = "teaching them to observe all things whatsoever I commanded you: and lo, I am with you always, even unto the end of the world."; Translation = "ASV"; Testament = "NT"; BookNumber = 40 }
    @{ Book = "John"; Chapter = 1; Verse = 1; Text = "In the beginning was the Word, and the Word was with God, and the Word was God."; Translation = "ASV"; Testament = "NT"; BookNumber = 43 }
    @{ Book = "John"; Chapter = 1; Verse = 14; Text = "And the Word became flesh, and dwelt among us (and we beheld his glory, glory as of the only begotten from the Father), full of grace and truth."; Translation = "ASV"; Testament = "NT"; BookNumber = 43 }
    @{ Book = "John"; Chapter = 3; Verse = 16; Text = "For God so loved the world, that he gave his only begotten Son, that whosoever believeth on him should not perish, but have eternal life."; Translation = "ASV"; Testament = "NT"; BookNumber = 43 }
    @{ Book = "John"; Chapter = 3; Verse = 17; Text = "For God sent not the Son into the world to judge the world; but that the world should be saved through him."; Translation = "ASV"; Testament = "NT"; BookNumber = 43 }
    @{ Book = "John"; Chapter = 14; Verse = 6; Text = "Jesus saith unto him, I am the way, and the truth, and the life: no one cometh unto the Father, but by me."; Translation = "ASV"; Testament = "NT"; BookNumber = 43 }
    @{ Book = "John"; Chapter = 14; Verse = 27; Text = "Peace I leave with you; my peace I give unto you: not as the world giveth, give I unto you. Let not your heart be troubled, neither let it be fearful."; Translation = "ASV"; Testament = "NT"; BookNumber = 43 }
    @{ Book = "Romans"; Chapter = 3; Verse = 23; Text = "for all have sinned, and fall short of the glory of God;"; Translation = "ASV"; Testament = "NT"; BookNumber = 45 }
    @{ Book = "Romans"; Chapter = 5; Verse = 8; Text = "But God commendeth his own love toward us, in that, while we were yet sinners, Christ died for us."; Translation = "ASV"; Testament = "NT"; BookNumber = 45 }
    @{ Book = "Romans"; Chapter = 6; Verse = 23; Text = "For the wages of sin is death; but the free gift of God is eternal life in Christ Jesus our Lord."; Translation = "ASV"; Testament = "NT"; BookNumber = 45 }
    @{ Book = "Romans"; Chapter = 8; Verse = 28; Text = "And we know that to them that love God all things work together for good, even to them that are called according to his purpose."; Translation = "ASV"; Testament = "NT"; BookNumber = 45 }
    @{ Book = "Romans"; Chapter = 8; Verse = 38; Text = "For I am persuaded, that neither death, nor life, nor angels, nor principalities, nor things present, nor things to come, nor powers,"; Translation = "ASV"; Testament = "NT"; BookNumber = 45 }
    @{ Book = "Romans"; Chapter = 8; Verse = 39; Text = "nor height, nor depth, nor any other creature, shall be able to separate us from the love of God, which is in Christ Jesus our Lord."; Translation = "ASV"; Testament = "NT"; BookNumber = 45 }
    @{ Book = "Romans"; Chapter = 12; Verse = 1; Text = "I beseech you therefore, brethren, by the mercies of God, to present your bodies a living sacrifice, holy, acceptable to God, which is your spiritual service."; Translation = "ASV"; Testament = "NT"; BookNumber = 45 }
    @{ Book = "Romans"; Chapter = 12; Verse = 2; Text = "And be not fashioned according to this world: but be ye transformed by the renewing of your mind, that ye may prove what is the good and acceptable and perfect will of God."; Translation = "ASV"; Testament = "NT"; BookNumber = 45 }
    @{ Book = "1 Corinthians"; Chapter = 13; Verse = 4; Text = "Love suffereth long, and is kind; love envieth not; love vaunteth not itself, is not puffed up,"; Translation = "ASV"; Testament = "NT"; BookNumber = 46 }
    @{ Book = "1 Corinthians"; Chapter = 13; Verse = 13; Text = "But now abideth faith, hope, love, these three; and the greatest of these is love."; Translation = "ASV"; Testament = "NT"; BookNumber = 46 }
    @{ Book = "Galatians"; Chapter = 2; Verse = 20; Text = "I have been crucified with Christ; and it is no longer I that live, but Christ living in me: and that life which I now live in the flesh I live in faith, the faith which is in the Son of God, who loved me, and gave himself up for me."; Translation = "ASV"; Testament = "NT"; BookNumber = 48 }
    @{ Book = "Galatians"; Chapter = 5; Verse = 22; Text = "But the fruit of the Spirit is love, joy, peace, longsuffering, kindness, goodness, faithfulness,"; Translation = "ASV"; Testament = "NT"; BookNumber = 48 }
    @{ Book = "Galatians"; Chapter = 5; Verse = 23; Text = "meekness, self-control; against such there is no law."; Translation = "ASV"; Testament = "NT"; BookNumber = 48 }
    @{ Book = "Ephesians"; Chapter = 2; Verse = 8; Text = "for by grace have ye been saved through faith; and that not of yourselves, it is the gift of God;"; Translation = "ASV"; Testament = "NT"; BookNumber = 49 }
    @{ Book = "Ephesians"; Chapter = 2; Verse = 9; Text = "not of works, that no man should glory."; Translation = "ASV"; Testament = "NT"; BookNumber = 49 }
    @{ Book = "Ephesians"; Chapter = 6; Verse = 10; Text = "Finally, be strong in the Lord, and in the strength of his might."; Translation = "ASV"; Testament = "NT"; BookNumber = 49 }
    @{ Book = "Ephesians"; Chapter = 6; Verse = 11; Text = "Put on the whole armor of God, that ye may be able to stand against the wiles of the devil."; Translation = "ASV"; Testament = "NT"; BookNumber = 49 }
    @{ Book = "Philippians"; Chapter = 4; Verse = 6; Text = "In nothing be anxious; but in everything by prayer and supplication with thanksgiving let your requests be made known unto God."; Translation = "ASV"; Testament = "NT"; BookNumber = 50 }
    @{ Book = "Philippians"; Chapter = 4; Verse = 7; Text = "And the peace of God, which passeth all understanding, shall guard your hearts and your thoughts in Christ Jesus."; Translation = "ASV"; Testament = "NT"; BookNumber = 50 }
    @{ Book = "Philippians"; Chapter = 4; Verse = 13; Text = "I can do all things in him that strengtheneth me."; Translation = "ASV"; Testament = "NT"; BookNumber = 50 }
    @{ Book = "2 Timothy"; Chapter = 1; Verse = 7; Text = "For God gave us not a spirit of fearfulness; but of power and love and discipline."; Translation = "ASV"; Testament = "NT"; BookNumber = 55 }
    @{ Book = "2 Timothy"; Chapter = 3; Verse = 16; Text = "Every scripture inspired of God is also profitable for teaching, for reproof, for correction, for instruction which is in righteousness:"; Translation = "ASV"; Testament = "NT"; BookNumber = 55 }
    @{ Book = "Hebrews"; Chapter = 11; Verse = 1; Text = "Now faith is assurance of things hoped for, a conviction of things not seen."; Translation = "ASV"; Testament = "NT"; BookNumber = 58 }
    @{ Book = "Hebrews"; Chapter = 12; Verse = 1; Text = "Therefore let us also, seeing we are compassed about with so great a cloud of witnesses, lay aside every weight, and the sin which doth so easily beset us, and let us run with patience the race that is set before us,"; Translation = "ASV"; Testament = "NT"; BookNumber = 58 }
    @{ Book = "Hebrews"; Chapter = 12; Verse = 2; Text = "looking unto Jesus the author and perfecter of our faith, who for the joy that was set before him endured the cross, despising shame, and hath sat down at the right hand of the throne of God."; Translation = "ASV"; Testament = "NT"; BookNumber = 58 }
    @{ Book = "James"; Chapter = 1; Verse = 2; Text = "Count it all joy, my brethren, when ye fall into manifold temptations;"; Translation = "ASV"; Testament = "NT"; BookNumber = 59 }
    @{ Book = "James"; Chapter = 1; Verse = 3; Text = "knowing that the proving of your faith worketh patience."; Translation = "ASV"; Testament = "NT"; BookNumber = 59 }
    @{ Book = "James"; Chapter = 1; Verse = 5; Text = "But if any of you lacketh wisdom, let him ask of God, who giveth to all liberally and upbraideth not; and it shall be given him."; Translation = "ASV"; Testament = "NT"; BookNumber = 59 }
    @{ Book = "1 Peter"; Chapter = 5; Verse = 7; Text = "casting all your anxiety upon him, because he careth for you."; Translation = "ASV"; Testament = "NT"; BookNumber = 60 }
    @{ Book = "1 John"; Chapter = 1; Verse = 9; Text = "If we confess our sins, he is faithful and righteous to forgive us our sins, and to cleanse us from all unrighteousness."; Translation = "ASV"; Testament = "NT"; BookNumber = 62 }
    @{ Book = "1 John"; Chapter = 4; Verse = 8; Text = "He that loveth not knoweth not God; for God is love."; Translation = "ASV"; Testament = "NT"; BookNumber = 62 }
    @{ Book = "Revelation"; Chapter = 21; Verse = 4; Text = "and he shall wipe away every tear from their eyes; and death shall be no more; neither shall there be mourning, nor crying, nor pain, any more: the first things are passed away."; Translation = "ASV"; Testament = "NT"; BookNumber = 66 }
)

# Add Reference and FullText to each verse
$asvVerses | ForEach-Object {
    $_.Reference = "$($_.Book) $($_.Chapter):$($_.Verse)"
    $_.FullText = "$($_.Reference): $($_.Text)"
}

# YLT - Young's Literal Translation (1898)
$yltVerses = @(
    @{ Book = "Genesis"; Chapter = 1; Verse = 1; Text = "In the beginning of God's preparing the heavens and the earth --"; Translation = "YLT"; Testament = "OT"; BookNumber = 1 }
    @{ Book = "Genesis"; Chapter = 1; Verse = 2; Text = "the earth hath existed waste and void, and darkness is on the face of the deep, and the Spirit of God fluttering on the face of the waters,"; Translation = "YLT"; Testament = "OT"; BookNumber = 1 }
    @{ Book = "Genesis"; Chapter = 1; Verse = 3; Text = "and God saith, 'Let light be;' and light is."; Translation = "YLT"; Testament = "OT"; BookNumber = 1 }
    @{ Book = "Psalms"; Chapter = 23; Verse = 1; Text = "Jehovah is my shepherd, I do not lack,"; Translation = "YLT"; Testament = "OT"; BookNumber = 19 }
    @{ Book = "Psalms"; Chapter = 23; Verse = 2; Text = "In pastures of tender grass He causeth me to lie down, By waters of rest He doth lead me."; Translation = "YLT"; Testament = "OT"; BookNumber = 19 }
    @{ Book = "Psalms"; Chapter = 23; Verse = 3; Text = "My soul He refresheth, He leadeth me in paths of righteousness, For His name's sake,"; Translation = "YLT"; Testament = "OT"; BookNumber = 19 }
    @{ Book = "Psalms"; Chapter = 23; Verse = 4; Text = "Also -- Loss though I walk in a valley of death-shade, I fear no evil, for Thou art with me, Thy rod and Thy staff -- they comfort me."; Translation = "YLT"; Testament = "OT"; BookNumber = 19 }
    @{ Book = "Psalms"; Chapter = 23; Verse = 5; Text = "Thou arrangest before me a table, Over-against my adversaries, Thou hast anointed with oil my head, My cup is full!"; Translation = "YLT"; Testament = "OT"; BookNumber = 19 }
    @{ Book = "Psalms"; Chapter = 23; Verse = 6; Text = "Only -- Loss goodness and kindness pursue me, All the days of my life, And my dwelling is in the house of Jehovah, For a length of days!"; Translation = "YLT"; Testament = "OT"; BookNumber = 19 }
    @{ Book = "Proverbs"; Chapter = 3; Verse = 5; Text = "Trust unto Jehovah with all thy heart, And unto thine own understanding lean not."; Translation = "YLT"; Testament = "OT"; BookNumber = 20 }
    @{ Book = "Proverbs"; Chapter = 3; Verse = 6; Text = "In all thy ways know thou Him, And He doth make straight thy paths."; Translation = "YLT"; Testament = "OT"; BookNumber = 20 }
    @{ Book = "Isaiah"; Chapter = 40; Verse = 31; Text = "But those expecting Jehovah pass to power, They raise up the pinion as eagles, They run and are not fatigued, They go on and do not faint!"; Translation = "YLT"; Testament = "OT"; BookNumber = 23 }
    @{ Book = "John"; Chapter = 1; Verse = 1; Text = "In the beginning was the Word, and the Word was with God, and the Word was God;"; Translation = "YLT"; Testament = "NT"; BookNumber = 43 }
    @{ Book = "John"; Chapter = 1; Verse = 14; Text = "And the Word became flesh, and did tabernacle among us, and we beheld his glory, glory as of an only begotten of a father, full of grace and truth."; Translation = "YLT"; Testament = "NT"; BookNumber = 43 }
    @{ Book = "John"; Chapter = 3; Verse = 16; Text = "for God did so love the world, that His Son -- the only begotten -- He gave, that every one who is believing in him may not perish, but may have life age-during."; Translation = "YLT"; Testament = "NT"; BookNumber = 43 }
    @{ Book = "John"; Chapter = 14; Verse = 6; Text = "Jesus saith to him, 'I am the way, and the truth, and the life, no one doth come unto the Father, if not through me;"; Translation = "YLT"; Testament = "NT"; BookNumber = 43 }
    @{ Book = "Romans"; Chapter = 3; Verse = 23; Text = "for all did sin, and are come short of the glory of God --"; Translation = "YLT"; Testament = "NT"; BookNumber = 45 }
    @{ Book = "Romans"; Chapter = 5; Verse = 8; Text = "and God doth commend His own love to us, that, in our being still sinners, Christ did die for us;"; Translation = "YLT"; Testament = "NT"; BookNumber = 45 }
    @{ Book = "Romans"; Chapter = 6; Verse = 23; Text = "for the wages of the sin is death, and the gift of God is life age-during in Christ Jesus our Lord."; Translation = "YLT"; Testament = "NT"; BookNumber = 45 }
    @{ Book = "Romans"; Chapter = 8; Verse = 28; Text = "And we have known that to those loving God all things do work together for good, to those who are called according to purpose;"; Translation = "YLT"; Testament = "NT"; BookNumber = 45 }
    @{ Book = "Ephesians"; Chapter = 2; Verse = 8; Text = "for by grace ye are having been saved, through faith, and this not of you -- of God the gift,"; Translation = "YLT"; Testament = "NT"; BookNumber = 49 }
    @{ Book = "Ephesians"; Chapter = 2; Verse = 9; Text = "not of works, that no one may boast;"; Translation = "YLT"; Testament = "NT"; BookNumber = 49 }
    @{ Book = "Philippians"; Chapter = 4; Verse = 13; Text = "for all things I have strength, in Christ's strengthening me;"; Translation = "YLT"; Testament = "NT"; BookNumber = 50 }
    @{ Book = "Hebrews"; Chapter = 11; Verse = 1; Text = "And faith is of things hoped for a confidence, of matters not seen a conviction,"; Translation = "YLT"; Testament = "NT"; BookNumber = 58 }
    @{ Book = "1 John"; Chapter = 4; Verse = 8; Text = "he who is not loving did not know God, because God is love;"; Translation = "YLT"; Testament = "NT"; BookNumber = 62 }
)

# Add Reference and FullText to each verse
$yltVerses | ForEach-Object {
    $_.Reference = "$($_.Book) $($_.Chapter):$($_.Verse)"
    $_.FullText = "$($_.Reference): $($_.Text)"
}

# Convert to JSON and save
$asvJson = $asvVerses | ConvertTo-Json -Depth 3
$yltJson = $yltVerses | ConvertTo-Json -Depth 3

$asvJson | Out-File -FilePath "$dataPath\asv.json" -Encoding UTF8
$yltJson | Out-File -FilePath "$dataPath\ylt.json" -Encoding UTF8

Write-Host "Created ASV Bible data: $dataPath\asv.json ($($asvVerses.Count) verses)" -ForegroundColor Green
Write-Host "Created YLT Bible data: $dataPath\ylt.json ($($yltVerses.Count) verses)" -ForegroundColor Green
Write-Host ""
Write-Host "Bible translations available:" -ForegroundColor Cyan
Get-ChildItem "$dataPath\*.json" | ForEach-Object { Write-Host "  - $($_.Name)" }
