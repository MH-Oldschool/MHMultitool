using System;
using System.Collections.Generic;
using System.Text;

namespace APX_Viewer
{
    static class ConstantsLocales
    {
        public struct Pushpin
        {
            public float x;
            public float y;
            public int type;
        }

        public enum Locales { dummy, Fort, FnH, Desert, Swamp, Volcano, Jungle, Castle, Battleground, SpArena, Arena, Mountains, Town, Tower3, Tower2, Tower1, blank1, DesertNight, blank2, VolcanoNight, JungleNight}

        public static readonly List<List<int>> MapScales = new List<List<int>>
        {
            new List<int> {1, 1}, //dummy
            new List<int> {320, 310}, //fort
            new List<int> {190, 212 }, //FnH
            new List<int> {270, 245}, //desert
            new List<int> {290, 285}, //swamp
            new List<int> {270, 340}, //volcano
            new List<int> {245, 215}, //jungle
            new List<int> {145, 125}, //castle
            new List<int> {280, 340}, //battleground
            new List<int> {200, 200}, //special arena (todo)
            new List<int> {200,200} //arena (todo)
        };

        public static readonly List<List<int>> LocaleRooms = new List<List<int>> //these are in room number order in order for offsets in the rooms to work correctly; i.e., the game has this table too somewhere
        {
            new List<int> { 13, 14, 15, 17, 24, 27, 29, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87 }, //dummy, invalid rooms for MH1
            new List<int> { 10, 30, 28, 11, 12, 31}, //fort rooms
            new List<int> { 21, 39, 38, 33, 37, 40, 32, 41, 35, 36, 34, 42, 43}, //fnh rooms
            new List<int> { 50, 51, 45, 57, 52, 54, 47, 56, 49, 48, 53, 55, 7}, //desert rooms
            new List<int> { 67, 16, 68, 6, 44, 9, 72, 75, 70, 69, 71, 73, 74}, //swamp rooms
            new List<int> { 61, 63, 58, 64, 59, 60, 65, 66, 62, 8 }, //volcano rooms
            new List<int> { 4, 2, 46, 1, 18, 5, 19, 3, 26, 22, 23}, //jungle rooms
            new List<int> { 20, 25}, //schrade rooms
            new List<int> { 8, 88}, //Battleground
            new List<int> { 89, 91}, //special arena
            new List<int> { 89, 90} //arena
            //87 is kokoto
            //minegarde in the 70s?
        };
        public static readonly List<List<int>> LocaleRooms2 = new List<List<int>>
        {
            //new List<int> {},
            //new List<int> { 16 } //swamp rooms

            //new List<int> { [camp], 111}
        };
    }
}
