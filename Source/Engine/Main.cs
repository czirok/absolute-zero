﻿using System;
using System.Collections.Generic;

namespace AbsoluteZero {

    /// <summary>
    /// The interface component of the Absolute Zero chess engine. 
    /// </summary>
    partial class Zero : IEngine {

        /// <summary>
        /// Returns the best move as determined by the engine. This method may write 
        /// output to the terminal. 
        /// </summary>
        /// <param name="position">The position to analyse.</param>
        /// <returns>The best move as determined by the engine.</returns>
        public Int32 GetMove(Position position) {
            if (Restrictions.Output == OutputType.Standard) {
                Terminal.WriteLine(Format.PadRight("Depth", DepthWidth) + Format.PadRight("Value", ValueWidth) + "Principal Variation");
                Terminal.WriteLine("-----------------------------------------------------------------------");
            }

            Prepare();
            stopwatch.Reset();
            stopwatch.Start();
            Int32 move = SearchRoot(position);
            stopwatch.Stop();

            if (Restrictions.Output == OutputType.Standard) {
                Terminal.WriteLine("-----------------------------------------------------------------------");
                Double elapsed = stopwatch.Elapsed.TotalMilliseconds;
                Terminal.WriteLine("FEN: " + position.GetFEN());
                Terminal.WriteLine();
                Terminal.WriteLine(position.ToStringAppend(
                    "Absolute Zero " + 8 * IntPtr.Size + "-bit",
                    "Version " + Version,
                    String.Empty,
                    "Nodes visited: " + totalNodes,
                    "Search time: " + Format.Precision(elapsed) + " ms",
                    "Search speed: " + Format.Precision(totalNodes / elapsed) + " kN/s",
                    String.Empty,
                    "Quiescence nodes: " + Format.Precision(100D * quiescenceNodes / totalNodes, 2) + "%",
                    "Hash usage: " + Format.Precision(100D * table.Count / table.Capacity, 2) + "%",
                    "Hash cutoffs: " + Format.Precision(100D * hashCutoffs / hashProbes, 2) + "%",
                    "Static evaluation: " + Format.PrecisionAndSign(.01 * Evaluate(position), 2)
                    ));
                Terminal.WriteLine();
            }
            return move;
        }

        /// <summary>
        /// Returns a string that describes the given principal variation. 
        /// </summary>
        /// <param name="position">The position the principal variation is to be played on.</param>
        /// <param name="depth">The depth of the search that yielded the principal variation. </param>
        /// <param name="value">The value of the search that yielded the principal variation.</param>
        /// <param name="pv">The principle variation to describe.</param>
        /// <returns>A string that describes the given principal variation.</returns>
        private String GetPVString(Position position, Int32 depth, Int32 value, List<Int32> pv) {
            Boolean isMate = Math.Abs(value) > NearCheckmateValue;
            Int32 movesToMate = (CheckmateValue - Math.Abs(value) + 1) / 2;

            switch (Restrictions.Output) {
                case OutputType.Standard:
                    String depthString = Format.PadRight(depth, DepthWidth);
                    String valueString = Format.PrecisionAndSign(.01 * value, 2);
                    if (isMate)
                        valueString = (value > 0 ? "+Mate " : "-Mate ") + movesToMate;
                    valueString = Format.PadRight(valueString, ValueWidth);
                    String movesString = Identify.MovesAlgebraically(position, pv);

                    return depthString + valueString + movesString;
                case OutputType.Universal:
                    String score = "cp " + value;
                    if (isMate)
                        score = "mate " + (value < 0 ? "-" : String.Empty) + movesToMate;
                    Double elapsed = stopwatch.Elapsed.TotalMilliseconds;
                    Int64 nps = (Int64)(1000 * totalNodes / elapsed);

                    return "info depth " + depth + " score " + score + " time " + (Int32)elapsed + " nodes " + totalNodes + " nps " + nps + " pv " + Identify.Moves(pv);
            }
            return String.Empty;
        }

        /// <summary>
        /// Collects and returns the principal variation for the last performed 
        /// search at the given depth. 
        /// </summary>
        /// <param name="position">The position the principal variation is to be played on.</param>
        /// <param name="depth">The depth of the search to collect the principal variation for.</param>
        /// <param name="firstMove">The first move of the principal variation.</param>
        /// <returns></returns>
        private List<Int32> CollectPV(Position position, Int32 depth, Int32 firstMove) {
            List<Int32> variation = new List<Int32>(depth);
            variation.Add(firstMove);
            for (Int32 i = 0; i < pvLength[1]; i++)
                variation.Add(pvMoves[1][i]);
            return variation;
        }

        /// <summary>
        /// Returns the principal variation for the last completed search. 
        /// </summary>
        /// <returns>The principal variation for the last completed search.</returns>
        public List<Int32> GetPV() {
            return pv;
        }

        /// <summary>
        /// Returns the number of nodes visited during the most recent search, which 
        /// may be an ongoing search or the last completed search. 
        /// </summary>
        /// <returns>The number of nodes visited during the most recent search.</returns>
        public Int64 GetNodes() {
            return totalNodes;
        }

        /// <summary>
        /// Sets the size of the transposition table to the given size in megabytes. 
        /// Unless the given size is the same as the current size, all entries in the 
        /// table are removed. 
        /// </summary>
        /// <param name="megabytes">The number of megabytes to allocate for the tranposition table.</param>
        public void AllocateHash(Int32 megabytes) {
            if (megabytes != table.Size >> 20)
                table = new HashTable(megabytes);
        }

        /// <summary>
        /// Terminates the ongoing search if applicable. 
        /// </summary>
        public void Stop() {
            abortSearch = true;
        }

        /// <summary>
        /// Resets the engine to its initial state. 
        /// </summary>
        public void Reset() {
            table.Clear();
            for (Int32 i = 0; i < killerMoves.Length; i++)
                Array.Clear(killerMoves[i], 0, killerMoves[i].Length);
            finalAlpha = 0;
            rootAlpha = 0;
        }

        /// <summary>
        /// Returns whether the engine is willing to accept a draw offer. 
        /// </summary>
        /// <returns>Whether the engine is willing to accept a draw offer.</returns>
        public Boolean AcceptDraw() {
            return finalAlpha <= DrawValue;
        }

        /// <summary>
        /// Returns the name of the engine. 
        /// </summary>
        /// <returns></returns>
        public String GetName() {
            return "Absolute Zero " + Version;
        }

        /// <summary>
        /// Resets fields to prepare for a new search. 
        /// </summary>
        private void Prepare() {
            abortSearch = false;
            totalNodes = 0;
            quiescenceNodes = 0;
            referenceNodes = 0;
            hashProbes = 0;
            hashCutoffs = 0;
        }
    }
}
