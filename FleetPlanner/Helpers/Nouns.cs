﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Helpers
{
    public static class Nouns
    {
        /// <summary>
        /// The values
        /// </summary>
        public static readonly List<string> Values = new List<string>()
        {
        "Abacus",
        "Abolishment",
        "Abuse",
        "Accent",
        "Accommodation",
        "Account",
        "Acetate",
        "Acquaintance",
        "Action",
        "Actress",
        "Addiction",
        "Admin",
        "Adoption",
        "Advancement",
        "Advertising",
        "Affair",
        "Afoul",
        "Afterthought",
        "Aggradation",
        "Agriculture",
        "Airbag",
        "Airforce",
        "Airship",
        "Alcohol",
        "Algebra",
        "Allergist",
        "Alluvium",
        "Alpenhorn",
        "Altitude",
        "Amazon",
        "Ambulance",
        "Amount",
        "Analogue",
        "Anarchist",
        "Android",
        "Angora",
        "Ankle",
        "Answer",
        "Anthropology",
        "Antique",
        "Anybody",
        "Aperitif",
        "Appeal",
        "Appetite",
        "Appliance",
        "Approach",
        "Apse",
        "Archaeologist",
        "Architecture",
        "Argument",
        "Armchair",
        "Armrest",
        "Arrival",
        "Article",
        "Ascent",
        "Asparagus",
        "Assault",
        "Assignment",
        "Association",
        "Astrolabe",
        "Atelier",
        "Atmosphere",
        "Attacker",
        "Attention",
        "Attraction",
        "Auditorium",
        "Authority",
        "Automaton",
        "Avocado",
        "Azimuth",
        "Backbone",
        "Backyard",
        "Bag",
        "Bagpipe",
        "Bakery",
        "Balcony",
        "Ballot",
        "Band",
        "Bangle",
        "Banking",
        "Baobab",
        "Barbiturate",
        "Bark",
        "Barrel",
        "Baseball",
        "Basin",
        "Bassoon",
        "Bathroom",
        "Batting",
        "Bead",
        "Beanie",
        "Beat",
        "Bedroom",
        "Beggar",
        "Behaviour",
        "Belfry",
        "Belly",
        "Benefit",
        "Bias",
        "Bidding",
        "Bikini",
        "Binoculars",
        "Birch",
        "Birth",
        "Bitten",
        "Bladder",
        "Blazer",
        "Blister",
        "Blogger",
        "Blossom",
        "Blueberry",
        "Boatyard",
        "Bomb",
        "Bondsman",
        "Bonus",
        "Booking",
        "Boon",
        "Bootie",
        "Borrowing",
        "Bottle",
        "Bough",
        "Bourgeoisie",
        "Bowl",
        "Boxer",
        "Boyhood",
        "Bracket",
        "Brand",
        "Breadcrumb",
        "Breakpoint",
        "Breeze",
        "Briefly",
        "Broccoli",
        "Bronchitis",
        "Brother",
        "Browsing",
        "Bubble",
        "Buffet",
        "Bulb",
        "Bullfighter",
        "Bungalow",
        "Burglar",
        "Burrito",
        "Bush",
        "Butcher",
        "Buying",
        "Cabinet",
        "Caddy",
        "Cage",
        "Calculus",
        "Calico",
        "Campaign",
        "Can",
        "Candidate",
        "Canoe",
        "Canvas",
        "Caper",
        "Cappelletti",
        "Car",
        "Carbon",
        "Care",
        "Carnival",
        "Carport",
        "Cartel",
        "Carving",
        "Cashier",
        "Cassock",
        "Catacomb",
        "Catastrophe",
        "Cation",
        "Causeway",
        "Cclamp",
        "Celeriac",
        "Celsius",
        "Cent",
        "Century",
        "Certainty",
        "Chain",
        "Chairperson",
        "Challenge",
        "Chandelier",
        "Chapel",
        "Charge",
        "Chart",
        "Chateau",
        "Checkbook",
        "Cheek",
        "Chem",
        "Chest",
        "Child",
        "Chime",
        "Chive",
        "Cholesterol",
        "Chopsticks",
        "Chrome",
        "Chub",
        "Cigarette",
        "Circle",
        "Cirrhosis",
        "Citrus",
        "Clamp",
        "Clarinet",
        "Classroom",
        "Claw",
        "Cleavage",
        "Clerk",
        "Clinic",
        "Cloakroom",
        "Clone",
        "Clothes",
        "Clover",
        "Clutch",
        "Coaster",
        "Cockpit",
        "Codepage",
        "Cofactor",
        "Coil",
        "Coleslaw",
        "Collar",
        "Collector",
        "Colonialism",
        "Colorlessness",
        "Combat",
        "Comestible",
        "Command",
        "Commission",
        "Commotion",
        "Commuter",
        "Compensation",
        "Complement",
        "Complicity",
        "Composite",
        "Compromise",
        "Concentration",
        "Conclusion",
        "Conduct",
        "Confidence",
        "Confusion",
        "Congressman",
        "Consciousness",
        "Consideration",
        "Consonant",
        "Constraint",
        "Consumer",
        "Container",
        "Contingency",
        "Contrail",
        "Controller",
        "Conversation",
        "Cook",
        "Coordination",
        "Copper",
        "Copywriter",
        "Corn",
        "Corporal",
        "Correspondent",
        "Cost",
        "Couch",
        "Councilperson",
        "Counselor",
        "Countess",
        "Coupon",
        "Courtroom",
        "Coverall",
        "Crackers",
        "Crap",
        "Crayon",
        "Creativity",
        "Credenza",
        "CremeBrulee",
        "Crewmate",
        "Cribbage",
        "Criteria",
        "Croissant",
        "Croup",
        "Cruise",
        "Crust",
        "Cue",
        "Culture",
        "Cupcake",
        "Curiosity",
        "Curriculum",
        "Curtain",
        "Custom",
        "Cyclamen",
        "Cymbal",
        "Dad",
        "Dairy",
        "Dance",
        "Dare",
        "Dashboard",
        "Daybed",
        "Dealing",
        "Debris",
        "Decency",
        "Declination",
        "Decrease",
        "Deed",
        "Defense",
        "Degree",
        "Demand",
        "Den",
        "Deodorant",
        "Deposit",
        "Deputy",
        "Desert",
        "Desktop",
        "Destruction",
        "Detective",
        "Devastation",
        "Deviance",
        "Diadem",
        "Dialogue",
        "Diary",
        "Dictionary",
        "Differential",
        "Digging",
        "Dilution",
        "Dinghy",
        "Diploma",
        "Director",
        "Disadvantage",
        "Discipline",
        "Discount",
        "Discussion",
        "Disguise",
        "Disparity",
        "Disposer",
        "Dissemination",
        "Distribution",
        "Diver",
        "Diving",
        "Doctor",
        "Doggie",
        "Dollop",
        "Domination",
        "Doorbell",
        "Doubling",
        "Downfall",
        "Downturn",
        "Dragster",
        "Drapes",
        "Dream",
        "Drill",
        "Driveway",
        "Drudgery",
        "Duckling",
        "Duffel",
        "DumpTruck",
        "Duplexer",
        "Duster",
        "Dwelling",
        "Dysfunction",
        "Earnings",
        "Earthquake",
        "Eavesdropper",
        "Ecology",
        "Ecumenist",
        "Edition",
        "Effect",
        "Effort",
        "Ego",
        "Election",
        "Elevator",
        "Elimination",
        "Elver",
        "Embellishment",
        "Emergent",
        "Empire",
        "Empowerment",
        "Encounter",
        "Endoderm",
        "Energy",
        "Engineering",
        "Enterprise",
        "Entrance",
        "Environment",
        "Ephemera",
        "Epithelium",
        "Equation",
        "Eraser",
        "Escalator",
        "Essay",
        "Estrogen",
        "Ethnicity",
        "Evaporation",
        "Everything",
        "Exaggeration",
        "Exasperation",
        "Exchange",
        "Execution",
        "Exhaustion",
        "Existence",
        "Expectancy",
        "Experiment",
        "Explosion",
        "Expression",
        "Eye",
        "Eyelash",
        "Eyestrain",
        "Facet",
        "Factory",
        "Fairness",
        "Fame",
        "Fannypack",
        "Farmland",
        "Father",
        "Fault",
        "Fear",
        "Federation",
        "Feel",
        "Fencing",
        "Ferryboat",
        "Fiber",
        "Ficlet",
        "Fig",
        "File",
        "Film",
        "Finding",
        "Finish",
        "Fireplace",
        "Fishing",
        "Fix",
        "Flan",
        "Flavor",
        "Flesh",
        "Flipflops",
        "Floozie",
        "Fluke",
        "Foal",
        "Fold",
        "Fondue",
        "Foot",
        "Footrest",
        "Forager",
        "Forecast",
        "Forever",
        "Format",
        "Forte",
        "Founder",
        "Fraction",
        "Fraud",
        "Freelance",
        "Freon",
        "Friend",
        "Fringe",
        "Frosting",
        "Fuck",
        "Functionality",
        "Fur",
        "Gadget",
        "Gallbladder",
        "Gambling",
        "Gang",
        "Garden",
        "Gasket",
        "Gate",
        "Gauge",
        "Gearshift",
        "Gem",
        "Genetics",
        "Gentleman",
        "Gesture",
        "Gig",
        "Girdle",
        "Gladiolus",
        "Gliding",
        "Glove",
        "Glutamate",
        "Goddess",
        "Going",
        "Goodbye",
        "Gosling",
        "Gown",
        "Graduation",
        "Grammar",
        "Grandma",
        "Grandson",
        "Graph",
        "Gravel",
        "Greatness",
        "Grid",
        "Gripper",
        "Grouper",
        "Guard",
        "Guestbook",
        "Guilt",
        "Gumshoe",
        "Gymnast",
        "Habitat",
        "Hair",
        "Halloween",
        "Hammock",
        "Handholding",
        "Handover",
        "Happiness",
        "Hardening",
        "Harmonica",
        "Harpsichord",
        "Hassock",
        "Hatchling",
        "Havoc",
        "Headlight",
        "Health",
        "Heartache",
        "Heartwood",
        "Hectare",
        "Heir",
        "Hellcat",
        "Hemisphere",
        "Hermit",
        "Hexagon",
        "Highland",
        "Hiking",
        "Hiring",
        "Hobbit",
        "Holder",
        "Homework",
        "Homosexuality",
        "Hood",
        "Horde",
        "Horseradish",
        "Hospitality",
        "Hotel",
        "Household",
        "Hovercraft",
        "Hug",
        "Hummus",
        "Hunger",
        "Hurry",
        "Hybridization",
        "Hydrofoil",
        "Hydroxyl",
        "Hypothesis",
        "Icicle",
        "Identity",
        "Ignorance",
        "Illustration",
        "Immigrant",
        "Implement",
        "Impress",
        "Impudence",
        "Incandescence",
        "Incidence",
        "Incompetence",
        "Index",
        "Inequality",
        "Infiltration",
        "Influx",
        "Ingrate",
        "Inhibitor",
        "Injunction",
        "Inlay",
        "Input",
        "Insight",
        "Inspiration",
        "Institution",
        "Insurance",
        "Integrity",
        "Intention",
        "Interface",
        "Interval",
        "Intestine",
        "Invention",
        "Investigation",
        "Invite",
        "Irony",
        "Isolation",
        "Jackfruit",
        "Jar",
        "Jeep",
        "Jeweller",
        "Jockey",
        "Journal",
        "Judgment",
        "Jumbo",
        "Junk",
        "Justification",
        "Kayak",
        "Kendo",
        "Kettledrum",
        "Kickoff",
        "Killer",
        "Kimono",
        "Kiosk",
        "Kneejerk",
        "Knitting",
        "Knuckle",
        "Labor",
        "Lace",
        "Ladle",
        "Lament",
        "Landing",
        "Lap",
        "Lard",
        "Latency",
        "Laugh",
        "Lawmaker",
        "Layout",
        "Leaker",
        "Leaver",
        "Legacy",
        "Legislature",
        "Lemonade",
        "Lentil",
        "Letter",
        "Liability",
        "Licence",
        "Lie",
        "Lift",
        "Ligula",
        "Lime",
        "Line",
        "Lining",
        "Lipid",
        "Listening",
        "Litter",
        "Loading",
        "Lobotomy",
        "Locket",
        "Loggia",
        "Loincloth",
        "Look",
        "Lord",
        "Lounge",
        "Luck",
        "Luncheonette",
        "Lute",
        "Lymphocyte",
        "Macadamia",
        "Macrame",
        "Maestro",
        "Mail",
        "Mainland",
        "Majority",
        "Making",
        "Mama",
        "Manager",
        "Mango",
        "Manifestation",
        "Mansard",
        "Mantua",
        "Maple",
        "March",
        "Marimba",
        "Marketer",
        "Marmalade",
        "Mascara",
        "Mast",
        "Match",
        "Mathematics",
        "Maybe",
        "Meaning",
        "Measurement",
        "Mechanic",
        "Median",
        "Melatonin",
        "Meme",
        "Menopause",
        "Merchandise",
        "Merit",
        "Metabolite",
        "Meteorology",
        "Metric",
        "Micronutrient",
        "Middleman",
        "Migrant",
        "Milestone",
        "Millet",
        "Mime",
        "Mineral",
        "Minion",
        "Mint",
        "Misnomer",
        "Missile",
        "Miter",
        "Moai",
        "Moccasins",
        "Modeling",
        "Molar",
        "Moment",
        "Monitoring",
        "Monsoon",
        "Moonlight",
        "Morbidity",
        "Mortality",
        "Motherinlaw",
        "Motorcar",
        "Moustache",
        "Movie",
        "Mug",
        "Muscatel",
        "Music",
        "Mussel",
        "Mutton",
        "Nail",
        "Narrative",
        "Necessity",
        "Nectarine",
        "Negotiation",
        "Neologism",
        "Nest",
        "Netbook",
        "News",
        "Nexus",
        "Nickname",
        "Nightlife",
        "Nitrogen",
        "Noise",
        "Noodle",
        "Nose",
        "Nothing",
        "Noun",
        "Nuke",
        "Nurse",
        "Nutmeg",
        "Oar",
        "Obedience",
        "Obligation",
        "Obsidian",
        "Ocean",
        "Odometer",
        "Offer",
        "Offset",
        "Omega",
        "Onion",
        "Operation",
        "Opportunist",
        "Optimization",
        "Orchid",
        "Organ",
        "Orient",
        "Osmosis",
        "Outfielder",
        "Outlet",
        "Outrigger",
        "Overcharge",
        "Overhead",
        "Oversight",
        "Oxford",
        "Package",
        "Paddock",
        "Painter",
        "Palace",
        "Pancake",
        "Panpipe",
        "Pants",
        "Papaya",
        "Parachute",
        "Paramecium",
        "Parchment",
        "Parenting",
        "Parsley",
        "Particle",
        "Passbook",
        "Pasta",
        "Pasture",
        "Pathogenesis",
        "Patriarch",
        "Patrolling",
        "Pavement",
        "Payee",
        "Peach",
        "Peasant",
        "Peen",
        "Penalty",
        "Penicillin",
        "Pentagon",
        "Percent",
        "Perfume",
        "Perp",
        "Perspective",
        "Petticoat",
        "Phenomenon",
        "Philosophy",
        "Photograph",
        "Phrasing",
        "Piccolo",
        "Pickle",
        "Piety",
        "Pile",
        "Pillbox",
        "Pin",
        "Pinecone",
        "Pint",
        "Pipeline",
        "Pit",
        "Pith",
        "Placode",
        "Planning",
        "Plaster",
        "Platinum",
        "Playroom",
        "Pledge",
        "Plot",
        "Plugin",
        "Plywood",
        "Pod",
        "Poignance",
        "Polarisation",
        "Policeman",
        "Poll",
        "Polyester",
        "Poncho",
        "Popsicle",
        "Porch",
        "Portion",
        "Post",
        "Postfix",
        "Pouch",
        "Powder",
        "Praise",
        "Precipitation",
        "Prefix",
        "Premier",
        "Presence",
        "President",
        "Presume",
        "Pricing",
        "Principle",
        "Prison",
        "Probability",
        "Proceedings",
        "Procurement",
        "Productivity",
        "Progenitor",
        "Progression",
        "Promise",
        "Propaganda",
        "Proportion",
        "Prosecution",
        "Prostanoid",
        "Protocol",
        "Prow",
        "Pseudoscience",
        "Pub",
        "Pudding",
        "Pump",
        "Punch",
        "Pupil",
        "Purity",
        "Push",
        "Pyridine",
        "Quarter",
        "Quest",
        "Quicksand",
        "Quiver",
        "Racing",
        "Radio",
        "Rag",
        "Railroad",
        "Raincoat",
        "Rake",
        "Ranch",
        "Ranger",
        "Ratepayer",
        "Ravioli",
        "Reactant",
        "Reading",
        "Rear",
        "Recall",
        "Recess",
        "Reclamation",
        "Recorder",
        "Rectangle",
        "Reduction",
        "Reflection",
        "Refund",
        "Region",
        "Regret",
        "Relation",
        "Reliability",
        "Remains",
        "Renaissance",
        "Repeat",
        "Report",
        "Republic",
        "Rescue",
        "Reserve",
        "Resist",
        "Resource",
        "Restaurant",
        "Result",
        "Retina",
        "Return",
        "Revenge",
        "Revolution",
        "Rhubarb",
        "Riddle",
        "Rifle",
        "Rip",
        "Rite",
        "Road",
        "Robot",
        "Role",
        "Room",
        "Rostrum",
        "Rowboat",
        "Ruckus",
        "Ruin",
        "Rumor",
        "Runway",
        "Sabre",
        "Safari",
        "Sail",
        "Sake",
        "Salesman",
        "Samovar",
        "Sanctity",
        "Sandpaper",
        "Sash",
        "Saucer",
        "Savings",
        "Scale",
        "Scanner",
        "Scene",
        "Schema",
        "Scholarship",
        "Scientist",
        "Score",
        "Scrambled",
        "Screening",
        "Scrim",
        "Sculpting",
        "Seaplane",
        "Seat",
        "Secretion",
        "Sediment",
        "Segment",
        "Selfesteem",
        "Semicircle",
        "Senator",
        "Sensor",
        "Separation",
        "Sermon",
        "Servitude",
        "Setting",
        "Sex",
        "Shadowbox",
        "Shame",
        "Shareholder",
        "Shed",
        "Shield",
        "Ship",
        "Shirtdress",
        "Shoehorn",
        "Shofar",
        "Shopping",
        "Shortwave",
        "Show",
        "Shutdown",
        "Sideburns",
        "Siege",
        "Signal",
        "Signup",
        "Sill",
        "Sin",
        "Sip",
        "Site",
        "Skean",
        "Skin",
        "Skylight",
        "Slapstick",
        "Slaw",
        "Sleet",
        "Slime",
        "Slot",
        "Smock",
        "Smuggling",
        "Sneaker",
        "Snorer",
        "Snowmobiling",
        "Snuggle",
        "Sociology",
        "Softball",
        "Soldier",
        "Soliloquy",
        "Somebody",
        "Somewhere",
        "Sonnet",
        "Sorghum",
        "Soulmate",
        "Sourwood",
        "Sow",
        "Spade",
        "Sparerib",
        "Spawn",
        "Spec",
        "Spectacles",
        "Speed",
        "Spending",
        "Spill",
        "Spit",
        "Spokesman",
        "Spoon",
        "Spotlight",
        "Spread",
        "Sprinter",
        "Spy",
        "Stab",
        "Staff",
        "Staircase",
        "Stamen",
        "Standoff",
        "State",
        "Statistic",
        "Steak",
        "Stench",
        "Stepdaughter",
        "Stepson",
        "Stiletto",
        "Stitch",
        "Stomach",
        "Stopsign",
        "Storyboard",
        "Strand",
        "Strawberry",
        "Stress",
        "Strip",
        "Strudel",
        "Studio",
        "Sty",
        "Subcomponent",
        "Submitter",
        "Subset",
        "Substitution",
        "Succotash",
        "Suffocation",
        "Suitcase",
        "Summary",
        "Sunbonnet",
        "Sunglasses",
        "Sunset",
        "Supper",
        "Supporter",
        "Surgeon",
        "Surround",
        "Survivor",
        "Sustainment",
        "Swath",
        "Sweatsuit",
        "Swine",
        "Swivel",
        "Symmetry",
        "Synergy",
        "System",
        "Tabletop",
        "Tactile",
        "Takeout",
        "Talking",
        "Tandem",
        "Tanktop",
        "Taro",
        "Tatami",
        "Taxi",
        "Teaching",
        "Tech",
        "Tectonics",
        "Telescreen",
        "Temper",
        "Temptress",
        "Tennis",
        "Tentacle",
        "Terminology",
        "Territory",
        "Testament",
        "Textbook",
        "Theft",
        "Therapist",
        "Thesis",
        "Thinking",
        "Thought",
        "Thrift",
        "Thug",
        "Thunderhead",
        "Ticket",
        "Till",
        "Timeout",
        "Tin",
        "Tire",
        "Toaster",
        "Toffee",
        "Tolerance",
        "Tomography",
        "Tone",
        "Toot",
        "Tophat",
        "Tornado",
        "Tosser",
        "Tourism",
        "Townhouse",
        "Track",
        "Trade",
        "Trafficker",
        "Train",
        "Trance",
        "Transition",
        "Transport",
        "Trash",
        "Treat",
        "Trellis",
        "Trial",
        "Trigonometry",
        "Triumph",
        "Trophy",
        "Truck",
        "Truth",
        "Tuba",
        "Tuition",
        "Tuneup",
        "Turmeric",
        "Turnstile",
        "Tuxedo",
        "Twilight",
        "Twitter",
        "Ukulele",
        "Underclothes",
        "Underpass",
        "Underwire",
        "Union",
        "Update",
        "Urn",
        "Utensil",
        "Vaccine",
        "Valley",
        "Vanilla",
        "Variety",
        "Veal",
        "Veil",
        "Velodrome",
        "Venom",
        "Verb",
        "Version",
        "Vestment",
        "Viability",
        "Vice",
        "View",
        "Vine",
        "Vinyl",
        "Virtue",
        "Vision",
        "Vitality",
        "Vodka",
        "Volatility",
        "Volunteering",
        "Voyage",
        "Wagon",
        "Walk",
        "Walnut",
        "Wardrobe",
        "Warmth",
        "Washbasin",
        "Wasting",
        "Waterbed",
        "Waterskiing",
        "Weakness",
        "Webinar",
        "Wedge",
        "Weekend",
        "Wellbeing",
        "Wharf",
        "Whip",
        "Whisper",
        "Widget",
        "Wilderness",
        "Windchime",
        "Winery",
        "Winner",
        "Wiseguy",
        "Withdrawal",
        "Wont",
        "Word",
        "Workhorse",
        "Worry",
        "Wraparound",
        "Wrench",
        "Writer",
        "Yam",
        "Year",
        "Yin",
        "Youngster",
        "Zampone",
        "Ziggurat",
        "Zone"

        };
    }
}
