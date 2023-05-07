﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class PairSummary
    {


        public class SuitSummary
        {
            internal (int Min, int Max) _quality;

            public (int Min, int Max) Shape { get; set; }
            public (int Min, int Max) Points { get; set; }

            public (SuitQuality Min, SuitQuality Max) Quality
            {
                get { return ((SuitQuality)_quality.Min, (SuitQuality)_quality.Max); }
                set { _quality.Min = (int)value.Min; _quality.Max = (int)value.Max; }
            }

     //       public (int A, int B)? Keycards
      //      {
       //         get; set;
       //     }

            public bool? HaveQueen { get; set; }



            public SuitSummary(HandSummary.SuitSummary ss1, HandSummary.SuitSummary ss2)
            {
                this.Shape = AddRange(ss1.Shape, ss2.Shape, 13);
                // TODO: Think about this - intersect range?  Not sure...
                this._quality = IntersectRange(ss1._quality, ss2._quality); 
              //  this.Quality = (SuitQuality.Poor, SuitQuality.Solid);
              //  this.Keycards = null;
            }
            // TODO: There are other properties like "Stopped", "Has Ace", that can go here...
        }

        public static (int Min, int Max) AddRange((int Min, int Max) r1, (int Min, int Max) r2, int max)
        {
            return (Math.Min(max, r1.Min + r2.Min), Math.Min(max, r1.Max + r2.Max));
        }

        private static (int Min, int Max) IntersectRange((int Min, int Max) r1, (int Min, int Max) r2)
        {
            return (Math.Max(r1.Min, r2.Min), Math.Min(r1.Max, r2.Max));
        }

        public (int Min, int Max) HighCardPoints;
        public (int Min, int Max) StartingPoints;
        public Dictionary<Suit, SuitSummary> Suits;
        public List<Suit> ShownSuits = new List<Suit>();

        public PairSummary(HandSummary hs1, HandSummary hs2)
        {
            this.HighCardPoints = AddRange(hs1.HighCardPoints, hs2.HighCardPoints, 40);
            this.StartingPoints = AddRange(hs1.StartingPoints, hs2.StartingPoints, int.MaxValue);
     //       this.CountAces = other.CountAces;
      //      this.CountKings = other.CountKings;
            this.Suits = new Dictionary<Suit, SuitSummary>();
            foreach (Suit suit in BasicBidding.Strains)
            {
                Suits[suit] = new SuitSummary(hs1.Suits[suit], hs2.Suits[suit]);
                if (Suits[suit].Shape.Min > 0) { ShownSuits.Add(suit); }
            }
        }

        public static PairSummary Opponents(PositionState ps)
        {
            return new PairSummary(ps.LeftHandOpponent.PublicHandSummary, ps.RightHandOpponent.PublicHandSummary);
        }
    }
}