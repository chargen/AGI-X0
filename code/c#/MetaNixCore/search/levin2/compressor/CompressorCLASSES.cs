﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using MetaNix.search.levin;
using MetaNix.framework.misc;

namespace MetaNix.search.levin2.compressor {

    public sealed class CompressorRepresentation {
        public CompressorRepresentation(bool[] instructionMask) {
            this.instructionMask = instructionMask;
        }

        /* commented because not used
        public bool checkAllDisabled() {
            return countEnabledInstructionMaskBits() == 0;
        }
        
        public int countEnabledInstructionMaskBits() {
            return instructionMask.Count(v => v);
        }
         */

        public readonly bool[] instructionMask; // mask to enable or disabled instruction "slots"
    }
    

    /*
     * /brief Compressor to search for common patterns in programs
     * 
     * The programs are generated by the program search algorithm.
     * The algorithm is in the current implementation ALS.
     * 
     * This search algorithm works by enumerating masks for the instructions and searching for the same instruction sequences in different programs.
     */
    public sealed class Levin2Compressor : Levin<CompressorRepresentation> {

        // subprogram of the cut out part of a bigger program.
        // 
        // program index from which it got cut is stored with it 
        private sealed class SubprogramWithMetaInfo {
            public SubprogramWithMetaInfo(uint[] subProgram, int sourceProgramIdx, int cutStartIdx) {
                this.subProgram = subProgram;
                this.sourceProgramIdx = sourceProgramIdx;
                this.cutStartIdx = cutStartIdx;
            }

            public readonly uint[] subProgram;
            public readonly int sourceProgramIdx;
            public readonly int cutStartIdx; // index of the cut in sourceProgram(Idx)
        }

        public Levin2Compressor(IList<uint[]> programsToCompress, int maxLength) : base(maxLength) {
            this.programsToCompress = programsToCompress;
        }

        // has to be called after the whole levin enumeration procedure
        // sorts out the pattern and application of the pattern to the programs which yield the highest compression ratio
        public void finish() {
            // TODO< sort out the pattern which are the most worth to compress >

            IDictionary<bool[], uint> score = new Dictionary<bool[], uint>();

            foreach( var iPatternWithPrograms in programsWithIdByPattern ) {

                // score is the saved number of instructions for the new instruction we introduce to compress all programs.
                // the "number of the saved instructions" = 
                //     "number of active instructions in the pattern" *
                //     "number of equal subprograms"

                // TODO< subtract the number of required instructions after compression from "number of saved instructions"

                throw new NotImplementedException();

            }

            throw new NotImplementedException();
        }

        // find all programs where patterns match up
        protected override void apply(CompressorRepresentation representation) {
            // bitpatterns with just one instruction are pointless
            ////if( representation.countEnabledInstructionMaskBits() <= 1 )   return;

            bool[] activeRepresentation = representation.instructionMask;
            
            for( int sourceProgramIdx = 0; sourceProgramIdx < programsToCompress.Count; sourceProgramIdx++ ) {
                uint[] iProgram = programsToCompress[sourceProgramIdx];

                // we can't find the same instructions if the program is shorter than the pattern itself
                if( iProgram.Length > activeRepresentation.Length ) {
                    continue;
                }

                // iterte over cutStartIdx
                Debug.Assert(activeRepresentation.Length <= iProgram.Length);
                for ( int cutStartIdx = 0; cutStartIdx <= iProgram.Length - activeRepresentation.Length; cutStartIdx++ ) {
                    var cuttedSubprogramWithInfo = cutSubprogram(sourceProgramIdx, cutStartIdx, activeRepresentation.Length);

                    addToProgramsByPatternIfNotExistOrPatternMatches(activeRepresentation, cuttedSubprogramWithInfo);
                }
            }
        }

        // checks for the existence of a subprogram by the pattern and adds it if it matches up or if it is completely novel
        void addToProgramsByPatternIfNotExistOrPatternMatches(bool[] pattern, SubprogramWithMetaInfo subprogram) {
            if( programsWithIdByPattern.ContainsKey(pattern) ) {
                var subprogramsByPattern = programsWithIdByPattern[pattern].Item1;

                ProgramGroup matchingProgramGroup = null;
                foreach( ProgramGroup iProgramGroup in subprogramsByPattern ) {
                    if( iProgramGroup.checkMatchesSubprogram(pattern, subprogram) ) {
                        matchingProgramGroup = iProgramGroup;
                        break;
                    }
                }
                bool existsSame = matchingProgramGroup != null;
                
                if( existsSame ) {
                    // add a count to the ProgramGroup
                    matchingProgramGroup.incrementMatchCounter();
                }
                else {
                    // add it
                    subprogramsByPattern.Add(new ProgramGroup(subprogram));
                }
            }
            else {
                programsWithIdByPattern[pattern] =
                    new Tuple<IList<ProgramGroup>, uint>(
                        new List<ProgramGroup>{new ProgramGroup(subprogram)},
                        returnNewPatternId()
                    );
            }
        }

        // adds the cutted-subprogram to the dictionary
        /* commented because not used
        void addToProgramsByPattern(bool[] pattern, SubprogramWithMetaInfo subprogram) {
            if( programsByPattern.ContainsKey(pattern) ) {
                programsByPattern[pattern].Add(subprogram);
            }
            else {
                programsByPattern[pattern] = new List<SubprogramWithMetaInfo>() { subprogram };
            }
        }
         */
        
        protected override CompressorRepresentation decode(uint[] enumeratedEncoding) {
            return new CompressorRepresentation(translateEnumeratedEncodingToInstructionMask(enumeratedEncoding));
        }

        protected override void increment(ref uint[] enumeratedEncoding, out bool isLengthCompleted) {
            isLengthCompleted = false;
            
            // special case
            if( enumeratedEncoding.Length == 0 ) {
                isLengthCompleted = true;
                return;
            }

            bool[] enumeratedEncodingAsBool = translateEnumeratedEncodingToInstructionMask(enumeratedEncoding);
            int asNumber = BinaryConversion.base2ToInt(enumeratedEncodingAsBool);
            asNumber++;
            bool[] incrementedAsBase2 = BinaryConversion.intToBase2(asNumber, enumeratedEncoding.Length);
            bool isWraparound = incrementedAsBase2.All(v => !v); // check if all are false

            if( isWraparound ) {
                isLengthCompleted = true;
                return;
            }

            enumeratedEncoding = convToUint(incrementedAsBase2);
        }
        

        static bool[] translateEnumeratedEncodingToInstructionMask(uint[] enumeratedEncoding) {
            bool[] convertedEnumeratedEncoding  = convToBool(enumeratedEncoding);

            // first bit and last bit are always true, because we are just interested with instruction matches where the first and last pattern instruction are active
            bool[] result = new bool[enumeratedEncoding.Length + 2];
            result[0] = true;
            result[result.Length - 1] = true;
            Array.Copy(convertedEnumeratedEncoding, 0, result, 1, convertedEnumeratedEncoding.Length);
            
            return result;
        }

        static bool[] convToBool(uint[] arr) {
            return arr.Select(v => v != 0).ToArray();
        }

        static uint[] convToUint(bool[] arr) {
            return arr.Select(v => v ? 1u : 0).ToArray();
        }

        SubprogramWithMetaInfo cutSubprogram(int sourceProgramIdx, int cutStartIdx, int cutLength) {
            uint[] program = programsToCompress[sourceProgramIdx];

            uint[] subProgram = new uint[cutLength];
            Array.Copy(program, cutStartIdx, subProgram, 0, cutLength);

            return new SubprogramWithMetaInfo(subProgram, sourceProgramIdx,  cutStartIdx);
        }

        IDictionary<bool[], Tuple<IList<ProgramGroup>, uint>> programsWithIdByPattern = new Dictionary<bool[], Tuple<IList<ProgramGroup>, uint>>();

        uint patternIdCounter = 0;

        uint returnNewPatternId() {
            return patternIdCounter++;
        }

        // OPTIMIZATION< array of array is faster >
        IList<uint[]> programsToCompress; // all programs which are candidates for compression

        // group of programs with the same masked out parts by an instruction-enabling-mask
        private class ProgramGroup {
            public ProgramGroup(SubprogramWithMetaInfo subprogram) {
                addSubprogram(subprogram);
            }

            private ProgramGroup() {} // disable ctor for convinience

            public void addSubprogram(SubprogramWithMetaInfo subprogram) {
                subprograms.Add(subprogram);
            }

            // checks if the masked instructions match up with the subprogram(s)
            public bool checkMatchesSubprogram(bool[] instructionEnablingMask, SubprogramWithMetaInfo other) {
                SubprogramWithMetaInfo anySubprogram = subprograms[0]; // it is valid to match up to the first subprogram

                Debug.Assert(instructionEnablingMask.Length == other.subProgram.Length);
                Debug.Assert(anySubprogram.subProgram.Length == other.subProgram.Length);

                int patternIdx;
                for (patternIdx = 0; patternIdx < instructionEnablingMask.Length; patternIdx++) {
                    if (!instructionEnablingMask[patternIdx]) continue; // ignore disabled instructions

                    if (anySubprogram.subProgram[patternIdx] != other.subProgram[patternIdx]) {
                        break;
                    }
                }
                bool isSamePattern = patternIdx == instructionEnablingMask.Length;

                return isSamePattern;
            }

            public void incrementMatchCounter() {
                matchingCounter++;
            }

            public int matchingCounter = 1; // used to count how many times the same subprogram was found in programs

            public IList<SubprogramWithMetaInfo> subprograms = new List<SubprogramWithMetaInfo>();
        }
    }
}
