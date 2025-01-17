﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// ReSharper disable StringIndexOfIsCultureSpecific.1

namespace TestBots.Bridge
{
    internal class PBN
    {
        public const string Sides = "NESW";
        public const string SuitLetters = "SHDC";
        public const string CardRanks = " 23456789TJQKA";
        public const string UnknownCard = "0U";

        public static int CardRank(string card)
        {
            return CardRanks.IndexOf(card[0]);
        }

        public static BasicTests.BasicTest[] ImportTests(string text)
        {
            var tests = new List<BasicTests.BasicTest>();
            var contract = "";
            var dealerSeat = 0;
            var hands = new List<string>();
            var history = new List<string>();
            var tags = TokenizeTags(text);
            var name = "";

            foreach (var tag in tags)
            {
                switch (tag.Name)
                {
                    case "Event":
                        name = tag.Description;
                        break;
                    case "Deal":
                        contract = "";
                        dealerSeat = Sides.IndexOf(tag.Description.Substring(0, 1).ToUpper());
                        hands = ImportHands(dealerSeat, tag.Description);
                        history = new List<string>();
                        break;
                    case "Auction":
                    {
                        dealerSeat = Sides.IndexOf(tag.Description.ToUpper());
                        var bids = ImportBids(tag.Data);
                        history = new List<string>();
                        for (var i = 0; i < bids.Count; i++)
                        {
                            var bid = bids[i];
                            var seat = (dealerSeat + i) % 4;
                            var hand = hands[seat];
                            var seatName = Sides[seat];
                            var bidNumber = 1 + i / 4;
                            if (!IsUnknownHand(hand))
                            {
                                tests.Add(
                                    new BasicTests.BasicTest
                                    {
                                        history = history.ToArray(),
                                        hand = hand,
                                        bid = bid,
                                        type = $"{name} (Seat {seatName}, Bid {bidNumber})"
                                    }
                                );
                            }
                            history.Add(bid);
                        }
                        break;
                    }
                    case "Contract":
                        contract = tag.Description;
                        break;
                    case "Play":
                    {
                        var leadSeat = Sides.IndexOf(tag.Description.ToUpper());
                        var declarerSeat = (4 + leadSeat - 1) % 4;
                        var dummySeat = (leadSeat + 1) % 4;
                        var trick = new List<string>();
                        var trump = contract[1];
                        var plays = ImportPlays(trump, tag.Data);
                        for (var i = 0; i < plays.Count; i++)
                        {
                            var play = plays[i];
                            var seat = (leadSeat + i) % 4;
                            var hand = hands[seat];
                            var seatName = Sides[seat];
                            var playNumber = 1 + i / 4;
                            // Don't validate plays for unknown hands
                            // And don't validate dummy plays when declarer's hand is unknown
                            if (!IsUnknownHand(hand) && !(seat == dummySeat && IsUnknownHand(hands[(dummySeat + 2) % 4])))
                            {
                                tests.Add(
                                    new BasicTests.BasicTest
                                    {
                                        contract = contract,
                                        dealerSeat = dealerSeat,
                                        declarerSeat = declarerSeat,
                                        history = history.ToArray(),
                                        dummy = i > 0 ? seat == dummySeat ? hands[declarerSeat] : hands[dummySeat] : "",
                                        hand = hand,
                                        play = play,
                                        plays = plays.GetRange(0, i).ToArray(),
                                        type = $"{name} (Seat {seatName}, Trick {playNumber})"
                                    }
                                );
                            }
                            trick.Add(play);
                            // Remove played card from hand
                            var regex = new Regex(IsUnknownHand(hand) ? UnknownCard : play);
                            hands[seat] = regex.Replace(hands[seat], "", 1);
                            // Update lead seat if end of trick
                            if (i % 4 == 3)
                            {
                                var card = GetTopCard(trick, trump);
                                leadSeat = (leadSeat + trick.IndexOf(card)) % 4;
                                trick.Clear();
                            }
                        }
                        break;
                    }
                }
            }

            // Ignore all other tags
            return tests.ToArray();
        }

        private static List<string> ImportBids(List<string> bidLines)
        {
            return string.Join(" ", bidLines)
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(bid => bid.Replace('S', '♠').Replace('H', '♥').Replace('D', '♦').Replace('C', '♣'))
                .ToList();
        }

        private static List<string> ImportHands(int dealerSeat, string handsString)
        {
            var hands = new List<string> { "", "", "", "" };
            var handStrings = handsString.Substring(2).Split(' ');
            for (var i = 0; i < handStrings.Length; i++)
            {
                var seat = (dealerSeat + i) % 4;
                var handString = handStrings[i];
                if (handString == "-")
                {
                    hands[seat] = string.Join("", Enumerable.Repeat(UnknownCard, 13));
                    continue;
                }

                var hand = "";
                var suits = handString.Split('.');

                for (var j = 0; j < suits.Length; j++)
                    foreach (var card in suits[j])
                        hand = $"{card}{SuitLetters[j]}" + hand; // Put high cards on right to match Trickster Cards production behavior

                hands[seat] = hand;
            }

            // validate known hands are of the correct length with no shared cards
            var knownHands = hands.Where(h => !IsUnknownHand(h));
            foreach (var hand in knownHands)
            {
                if (hand.Length != 13 * 2)
                    throw new ArgumentException($"Hand without exactly 13 cards found in '{handsString}'");

                if (hands.Count(h => h == hand) > 1)
                    throw new ArgumentException($"Multiple identical hands found in '{handsString}'");

                for (var i = 0; i < hand.Length; i+=2)
                {
                    var card = hand.Substring(i, 2);
                    if (knownHands.Any(h => h != hand && h.Contains(card)))
                        throw new ArgumentException($"Multiple hands with {card} found in '{handsString}'");
                }
            }

            return hands;
        }

        public static string GetTopCard(List<string> trick, char trump)
        {
            var ledSuit = trick[0][1];
            return trick.Where(c => c[1] == trump).OrderByDescending(CardRank).FirstOrDefault()
                ?? trick.Where(c => c[1] == ledSuit).OrderByDescending(CardRank).First();
        }

        private static List<string> ImportPlays(char trump, List<string> playLines)
        {
            var plays = new List<string>();
            var leadSeat = 0;
            foreach (var line in playLines)
            {
                var cardPlays = line.ToUpper().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var cards = cardPlays.Select(cp => cp.Length == 2 ? string.Concat(cp[1], cp[0]) : "").ToList();

                var trick = new List<string>();
                for (var i = 0; i < 4; i++)
                {
                    var seat = (leadSeat + i) % 4;
                    if (!string.IsNullOrEmpty(cards[seat]))
                    {
                        trick.Add(cards[seat]);
                        plays.Add(cards[seat]);
                    }
                }

                var topCard = GetTopCard(trick, trump);
                leadSeat = cards.IndexOf(topCard);
            }

            return plays;
        }

        private static bool IsUnknownHand(string hand)
        {
            return hand.Substring(0, 2) == UnknownCard;
        }

        private static List<PBNTag> TokenizeTags(string text)
        {
            var tag = new PBNTag { Data = new List<string>() };
            var tags = new List<PBNTag>();
            var lines = text.Split(new[] { "\n", "\n\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("%"));
            foreach (var line in lines)
                if (line.StartsWith("["))
                {
                    tag = new PBNTag { Data = new List<string>() };
                    tags.Add(tag);
                    tag.Name = line.Substring(1, line.IndexOf(' ') - 1);
                    var start = line.IndexOf('"') + 1;
                    var end = line.LastIndexOf('"') - start;
                    tag.Description = line.Substring(start, end);
                }
                else
                {
                    tag.Data.Add(line);
                }

            return tags;
        }

        private class PBNTag
        {
            public List<string> Data { get; set; }
            public string Description { get; set; }
            public string Name { get; set; }
        }
    }
}