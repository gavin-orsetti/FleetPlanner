using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FleetPlanner.Helpers
{
    public static class NameGenerator
    {
        private static Random rand = new Random();
        private static int firstNamesCount = 8070;
        private static int FirstNamesCount => firstNamesCount;

        private static int lastNamesCount = 55368;
        private static int LastNamesCount => lastNamesCount;

        private static int animalsCount = 1158;
        private static int AnimalsCount => animalsCount;

        private static int adjectivesCount = 4839;
        private static int AdjectivesCount => adjectivesCount;

        private static int nounsCount = 5914;
        private static int NounsCount => nounsCount;

        private static int professionsCount = 409;
        private static int ProfessionsCount => professionsCount;

        //private static int? nationalitiesCount;
        //private static int NationalitiesCount = nationalitiesCount ??= Nationalities.Values.Count - 1;

        private static List<string> usedNames = [];
        private static List<string> usedNouns = [];
        private static List<string> usedAdjectives = [];
        private static List<string> usedProfessions = [];
        private static List<string> usedAnimals = [];

        private static List<string> usedNamedAnimals = [];
        private static List<string> usedNamedProfessionalAnimals = [];
        private static List<string> usedIdentifiers = [];

        public static string SplitCamelCase( this string source, char delimiter = ' ' )
        {
            return string.Join( delimiter, Regex.Split( source, @"(?<!^)(?=[A-Z])" ) );
        }

        public static string GetRandomFirstName( bool useOnce = false )
        {
            int random = rand.Next( maxValue: FirstNamesCount );
            string name = FirstNames.Values[ random ].SplitCamelCase();

            if( usedNames.Contains( name ) )
            {
                return GetRandomFirstName( useOnce );
            }

            if( useOnce )
                usedNames.Add( name );

            return name;
        }

        public static string GetRandomLastName( bool useOnce = false )
        {
            int random = rand.Next( maxValue: LastNamesCount );
            string name = LastNames.Values[ random ].SplitCamelCase();

            if( usedNames.Contains( name ) )
            {
                return GetRandomLastName( useOnce );
            }

            if( useOnce )
                usedNames.Add( name );


            return name;
        }
        public static string GetRandomAnimal( bool useOnce = false )
        {
            int random = rand.Next( maxValue: AnimalsCount );
            string animal = Animals.Values[ random ].SplitCamelCase();

            if( usedAnimals.Contains( animal ) )
            {
                return GetRandomAnimal( useOnce );
            }

            if( useOnce )
                usedAnimals.Add( animal );

            return animal;
        }

        public static string GetRandomProfession( bool useOnce = false )
        {
            int random = rand.Next( maxValue: ProfessionsCount );
            string profession = Professions.Values[ random ].SplitCamelCase();

            if( usedProfessions.Contains( profession ) )
            {
                return GetRandomProfession( useOnce );
            }

            if( useOnce )
                usedProfessions.Add( profession );

            return profession;
        }

        public static string GetRandomAdjective( bool useOnce = false )
        {
            int random = rand.Next( maxValue: AdjectivesCount );
            string adjective = Adjectives.Values[ random ].SplitCamelCase();

            if( usedAdjectives.Contains( adjective ) )
            {
                return GetRandomAdjective( useOnce );
            }

            if( useOnce )
                usedAdjectives.Add( adjective );

            return adjective;
        }

        //private static string GetRandomNationality()
        //{
        //    int random = rand.Next( maxValue: NationalitiesCount );
        //    string nationality = Nationalities.Values[ random ];

        //    return nationality;
        //}

        public static string GetRandomNoun( bool useOnce = false )
        {
            int random = rand.Next( maxValue: NounsCount );
            string noun = Nouns.Values[ random ].SplitCamelCase();

            if( usedNouns.Contains( noun ) )
            {
                return GetRandomNoun( useOnce );
            }

            if( useOnce )
                usedNouns.Add( noun );
            return noun;
        }

        public static string GetRandomTwoPartName( bool useOnce = false, bool useComponentsOnce = false )
        {
            string first = GetRandomFirstName( useComponentsOnce );
            string last = GetRandomLastName( useComponentsOnce );

            string name = $"{first} {last}";

            if( usedNames.Contains( first ) )
            {
                return GetRandomTwoPartName( useOnce );
            }

            if( useOnce )
                usedNames.Add( name );

            return name;
        }

        public static string GetRandomThreePartName( bool useOnce = false, bool useComponentsOnce = false )
        {
            string first = GetRandomFirstName( useComponentsOnce );
            string middle = GetRandomFirstName( useComponentsOnce );
            string last = GetRandomLastName( useComponentsOnce );
            string name = $"{first} {middle} {last}";

            if( usedNames.Contains( name ) )
            {
                return GetRandomThreePartName( useOnce );
            }

            if( useOnce )
                usedNames.Add( name );

            return name;

        }


        public static string GetRandomNamedAnimal( bool useOnce = false, bool useComponentsOnce = false, bool leadWithName = true )
        {
            string name = GetRandomFirstName( useComponentsOnce );
            string animal = GetRandomAnimal( useComponentsOnce );

            string namedAnimal = name + animal;
            if( usedNamedAnimals.Contains( namedAnimal ) )
            {
                return GetRandomNamedAnimal( useOnce, leadWithName );
            }

            if( useOnce )
                usedNamedAnimals.Add( namedAnimal );

            return leadWithName switch
            {
                false => $"The {animal} {name}",
                true => $"{name} the {animal}"
            };
        }

        public static string GetRandomNamedProfessionalAnimal( bool useOnce = false, bool useComponentsOnce = false, bool leadWithName = true )
        {
            string name = GetRandomFirstName( useComponentsOnce );
            string animal = GetRandomAnimal( useComponentsOnce );
            string profession = GetRandomProfession( useComponentsOnce );
            string namedProfessionalAnimal = name + animal + profession;

            if( usedNamedProfessionalAnimals.Contains( namedProfessionalAnimal ) )
            {
                return GetRandomNamedProfessionalAnimal();
            }

            if( useOnce )
                usedNamedProfessionalAnimals.Add( namedProfessionalAnimal );

            return leadWithName switch
            {
                true => $"{name} the {animal} ({profession})",
                false => $"The {animal} {name} ({profession})"
            };
        }

        public static string GetRandomTwoPartIdentifier( bool useOnce = true, bool useComponentsOnce = false )
        {
            string adjective = GetRandomAdjective( useComponentsOnce );
            string noun = GetRandomNoun( useComponentsOnce );

            string identifier = adjective + noun;

            if( usedIdentifiers.Contains( identifier ) )
            {
                return GetRandomTwoPartIdentifier();
            }

            if( useOnce )
                usedIdentifiers.Add( identifier );

            return $"{adjective} {noun}";
        }

        public static string GetRandomThreePartIdentifier( bool useOnce = true, bool useComponentsOnce = false )
        {
            string adjective_1 = GetRandomAdjective( useComponentsOnce );
            string adjective_2 = GetRandomAdjective( useComponentsOnce );
            string noun = GetRandomNoun( useComponentsOnce );

            string identifier = adjective_1 + adjective_2 + noun;

            if( usedIdentifiers.Contains( identifier ) )
            {
                return GetRandomTwoPartIdentifier();
            }

            if( useOnce )
                usedIdentifiers.Add( identifier );

            return $"{adjective_1} {adjective_2} {noun}";
        }

        /// <summary>
        /// Gets a randomly generated identifier made up of some number of names, adjectives, nouns, animals, and professions. All strings are guaranteed to be unique (within a single session).
        /// </summary>
        /// <param name="useOnce">Ensure a unique identifier</param>
        /// <param name="useComponentsOnce">ensure unique components</param>
        /// <param name="leadWithName">Put the name (if one is contained) at the beginning of the string</param>
        /// <returns></returns>
        public static string GetRandomIdentifier( bool useOnce = true, bool useComponentsOnce = false, bool leadWithName = true )
        {
            // This is the number of methods that can return a string to us. If we add more, we need to update this method with the new combinations and make sure this number matches.
            int random = rand.Next( minValue: 1, maxValue: 7 );

            return random switch
            {
                1 => GetRandomFirstName( useOnce ),
                2 => GetRandomTwoPartName( useOnce ),
                3 => GetRandomThreePartName( useOnce ),
                4 => GetRandomNamedAnimal( useOnce, leadWithName: leadWithName ),
                5 => GetRandomNamedProfessionalAnimal( useOnce, leadWithName: leadWithName ),
                6 => GetRandomTwoPartIdentifier( useOnce, useComponentsOnce ),
                7 => GetRandomThreePartIdentifier( useOnce, useComponentsOnce ),
            };
        }
    }
}
