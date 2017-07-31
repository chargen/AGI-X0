﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

using AiThisAndThat.lang;
using AiThisAndThat.patternMatching;
using AiThisAndThat.framework.pattern.withDecoration;

namespace AiThisAndThat.languages.functional2 {
    enum EnumToken {
        AT = 0, // @
        DOUBLENUMBERSIGN, // ##
        BRACEOPEN, // {
        BRACECLOSE, // }
        SEMICOLON, // ;
    }

    class Lexer : AiThisAndThat.lang.Lexer {
        protected override Token createToken(int ruleIndex, string matchedString) {
            Token token = new Token();

            if( ruleIndex == 1 ) {
                token.type = Token.EnumType.IDENTIFIER;
	    		token.contentString = matchedString;
            }
            else if( ruleIndex == 2 ) {
			    token.type = Token.EnumType.OPERATION;
			    token.contentOperation = (int)EnumToken.AT;
		    }
            else if( ruleIndex == 3 ) {
			    token.type = Token.EnumType.OPERATION;
			    token.contentOperation = (int)EnumToken.DOUBLENUMBERSIGN;
		    }
            else if( ruleIndex == 4 ) {
			    token.type = Token.EnumType.OPERATION;
			    token.contentOperation = (int)EnumToken.BRACEOPEN;
		    }
            else if( ruleIndex == 5 ) {
			    token.type = Token.EnumType.OPERATION;
			    token.contentOperation = (int)EnumToken.BRACECLOSE;
		    }
            else if( ruleIndex == 6 ) {
			    token.type = Token.EnumType.OPERATION;
			    token.contentOperation = (int)EnumToken.SEMICOLON;
		    }
            else if( ruleIndex == 7 ) {
                token.type = Token.EnumType.STRING;
	    		token.contentString = matchedString;
            }
            else if( ruleIndex == 8 ) {
                token.type = Token.EnumType.NUMBER;
	    		token.contentNumber = long.Parse(matchedString);
            }
            
            return token;
        }

        protected override void fillRules() {
            tokenRules = new Rule[] {
                new Rule("^([ \\n\\r\\t]+|\\-\\-[ \\r\\t0-9\\w\\s\\.,:;_\\-\"#]*\n)"), // space or comment
                new Rule(@"^([a-zA-Z/\-\?!=][0-9a-zA-Z/\-\?!=]*)"),
                new Rule(@"^(\@)"),
                new Rule(@"^(##)"),
                new Rule(@"^({)"),
                new Rule(@"^(})"),
                new Rule(@"^(;)"),
                new Rule("^\"([0-9\\._;:,\\w\\s\\-\\./#]*)\""), // string
                new Rule(@"^(\-?[1-9][0-9]*|0)"), // integer
            };
        }
    }

    class Functional2LexerAndParser : AiThisAndThat.lang.Parser {
        public Functional2LexerAndParser(PatternSymbolContext patternSymbolContext) : base() {
            this.patternSymbolContext = patternSymbolContext;
        }


        protected override void fillArcs() {
            Arc errorArc = new Arc(Functional2LexerAndParser.Arc.EnumType.ERROR    , 0                                                    , callbackNothing             , 0                     , null                     );

            /* +  0 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.OPERATION, (uint)EnumToken.AT             , callbackNothing       , 1, null));
            /* +  1 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.ARC      , 10                              , callbackNothing, 2, null));
            /* +  2 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.END      , 0                              , callbackNothing, uint.MaxValue, null));
            /* +  3 */this.arcs.Add(errorArc);
            /* +  4 */this.arcs.Add(errorArc);
            /* +  5 */this.arcs.Add(errorArc);
            /* +  6 */this.arcs.Add(errorArc);
            /* +  7 */this.arcs.Add(errorArc);
            /* +  8 */this.arcs.Add(errorArc);
            /* +  9 */this.arcs.Add(errorArc);
            
            Debug.Assert(this.arcs.Count == 10);

            // interpretation-pattern
            /* + 10 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.NIL      , 0                               , callbackNothing       , 11, null));
            /* + 11 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.TOKEN    , (uint)Token.EnumType.IDENTIFIER , callbackSetRuleType, 12, null));
            /* + 12 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.OPERATION, (uint)EnumToken.BRACEOPEN       , callbackEnterInterpretationPattern, 13, null));

            // interpretation-pattern or pattern
            //    inner variable
            /* + 13 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.OPERATION, (uint)EnumToken.DOUBLENUMBERSIGN, callbackNothing       , 14, 15));
            /* + 14 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.TOKEN    , (uint)Token.EnumType.IDENTIFIER , callbackAddVariable, 13, null));
		    
            //    inner interpretation-pattern
            /* + 15 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.OPERATION, (uint)EnumToken.AT              , callbackNothing       , 16, 19));
            /* + 16 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.ARC      , 10                              , callbackNothing, 13, null));

            /* + 17 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.OPERATION, (uint)EnumToken.BRACECLOSE      , callbackExitInterpretationPatternOrPattern, 18, null));
            
            /* + 18 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.END      , 0                               , callbackNothing, uint.MaxValue, null));

            /* + 19 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.TOKEN    , (uint)Token.EnumType.NUMBER, callbackAddLong, 13, 20));

            /* + 20 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.OPERATION, (uint)EnumToken.BRACEOPEN      , callbackEnterPattern, 21, 25));
            /* + 21 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.ARC      , 13                             , callbackNothing, 13, null));
            
            //    inner identifier
            /* + 22 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.TOKEN    , (uint)Token.EnumType.IDENTIFIER , callbackAddSymbol, 13, 17));
            /* + 23 */this.arcs.Add(errorArc);
            /* + 24 */this.arcs.Add(errorArc);

            //    inner instruction
            //          name parameter(variable or pattern or name) ;
            
            /* + 25 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.TOKEN    , (uint)Token.EnumType.STRING, callbackEnterInstructionAndInstructionName, 26, 22));
            /* + 26 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.OPERATION, (uint)EnumToken.SEMICOLON      , callbackExitInstruction, 13, 27));
            /* + 27 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.TOKEN    , (uint)Token.EnumType.STRING, callbackAddString, 26, 28));
            /* + 28 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.TOKEN    , (uint)Token.EnumType.STRING, callbackInstructionParameterString, 26, 30));
            /* + 29 */this.arcs.Add(errorArc);

            Debug.Assert(this.arcs.Count == 30);

            
            /* + 30 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.TOKEN    , (uint)Token.EnumType.NUMBER, callbackAddLong, 26, 31));
            
            /* + 31 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.OPERATION, (uint)EnumToken.DOUBLENUMBERSIGN, callbackNothing       , 32, 40));
            /* + 32 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.TOKEN    , (uint)Token.EnumType.IDENTIFIER , callbackAddVariable, 26, null));
            
            /* + 33 */this.arcs.Add(errorArc);
            /* + 34 */this.arcs.Add(errorArc);
            /* + 35 */this.arcs.Add(errorArc);
            /* + 36 */this.arcs.Add(errorArc);
            /* + 37 */this.arcs.Add(errorArc);
            /* + 38 */this.arcs.Add(errorArc);
            /* + 39 */this.arcs.Add(errorArc);

            Debug.Assert(this.arcs.Count == 40);

            //         pattern
            /* + 40 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.OPERATION, (uint)EnumToken.BRACEOPEN      , callbackEnterPattern, 41, null));
            /* + 41 */this.arcs.Add(new Arc(Functional2LexerAndParser.Arc.EnumType.ARC      , 13                             , callbackNothing, 26, null));
            /* + 42 */this.arcs.Add(errorArc);
            /* + 43 */this.arcs.Add(errorArc);
            /* + 44 */this.arcs.Add(errorArc);
            /* + 45 */this.arcs.Add(errorArc);
            /* + 46 */this.arcs.Add(errorArc);
            /* + 47 */this.arcs.Add(errorArc);
            /* + 48 */this.arcs.Add(errorArc);
            /* + 49 */this.arcs.Add(errorArc);
            
            
            Debug.Assert(this.arcs.Count == 50);
            
            
            

        }

        protected override void setupBeforeParsing() {
            topPatternStack.Clear();
        }

        void callbackNothing(AiThisAndThat.lang.Parser parser, Token currentToken) {
        }

        void callbackSetRuleType(AiThisAndThat.lang.Parser parser, Token currentToken) {
            string tokenText = currentToken.contentString;
            
            if( tokenText == "match" ) {
                ruleType = EnumRuleType.MATCH;
            }
            else if( tokenText == "tuple2" ) {
                ruleType = EnumRuleType.TUPLE2;
            }
            else if( tokenText == "seq" ) {
                ruleType = EnumRuleType.SEQUENCE;
            }
            else if( tokenText == "loop" ) {
                ruleType = EnumRuleType.LOOP;
            }
            else {
                throw new Exception("Parsing error: " + tokenText + " is not a valid rule type!");
            }
        }

        void callbackEnterInterpretationPattern(AiThisAndThat.lang.Parser parser, Token currentToken) {
            ulong uniqueId = patternSymbolContext.returnNewUniqueId();

            Pattern<AiThisAndThat.patternMatching.Decoration> interpretationPattern = Pattern<AiThisAndThat.patternMatching.Decoration>.makeBranch(uniqueId);
            interpretationPattern.decoration = new Decoration();

            interpretationPattern.referenced = new Pattern<AiThisAndThat.patternMatching.Decoration>[0];

            if( ruleType == EnumRuleType.MATCH ) {
                interpretationPattern.decoration.type = Decoration.EnumType.VARIABLETEMPLATEMATCHING;
                interpretationPattern.type = Pattern<Decoration>.EnumType.BRANCH;
            }
            else if( ruleType == EnumRuleType.TUPLE2 ) {
                interpretationPattern.decoration.type = Decoration.EnumType.TUPLE2;
                interpretationPattern.type = Pattern<Decoration>.EnumType.BRANCH;
            }
            else if( ruleType == EnumRuleType.SEQUENCE ) {
                interpretationPattern.decoration.type = Decoration.EnumType.SEQUENCE;
                interpretationPattern.type = Pattern<Decoration>.EnumType.BRANCH;
            }
            else if( ruleType == EnumRuleType.LOOP ) {
                interpretationPattern.decoration.type = Decoration.EnumType.LOOP;
                interpretationPattern.type = Pattern<Decoration>.EnumType.BRANCH;
            }
            else {
                throw new Exception("Internal error"); // invalid ruleType, is a bug
            }

            // special case because added pattern can be first pattern
            if( topPatternStack.Count != 0 ) {
                PatternManipulation.append(topPattern, interpretationPattern);
            }

            topPatternStack.Add(interpretationPattern); // TODO< rewrite to push >
        }

        void callbackExitInterpretationPatternOrPattern(AiThisAndThat.lang.Parser parser, Token currentToken) {
            // don't remove the root element
            if( topPatternStack.Count > 1 )   topPatternStack.RemoveAt(topPatternStack.Count-1); // TODO< rewrite to pop >
        }

        void callbackAddVariable(AiThisAndThat.lang.Parser parser, Token currentToken) {
            string variableName = currentToken.contentString;

            ulong uniqueId = patternSymbolContext.lookupOrCreateUniqueIdForVariable(variableName);

            Pattern<AiThisAndThat.patternMatching.Decoration> variablePattern = Pattern<AiThisAndThat.patternMatching.Decoration>.makeVariable(uniqueId);
            
            PatternManipulation.append(topPattern, variablePattern);
        }

        void callbackEnterPattern(AiThisAndThat.lang.Parser parser, Token currentToken) {
            ulong uniqueId = patternSymbolContext.returnNewUniqueId();

            Pattern<AiThisAndThat.patternMatching.Decoration> interpretationPattern = Pattern<AiThisAndThat.patternMatching.Decoration>.makeBranch(uniqueId);
            interpretationPattern.referenced = new Pattern<AiThisAndThat.patternMatching.Decoration>[0];

            PatternManipulation.append(topPattern, interpretationPattern);

            topPatternStack.Add(interpretationPattern); // TODO< rewrite to push >
        }

        //void callbackExitPattern(AiThisAndThat.lang.Parser parser, Token currentToken) {
        //    topPatternStack.RemoveAt(topPatternStack.Count-1); // TODO< rewrite to pop >
        //}

        void callbackAddSymbol(AiThisAndThat.lang.Parser parser, Token currentToken) {
            string symbolName = currentToken.contentString;

            Tuple<ulong, ulong> symbolIdAndUniqueId = patternSymbolContext.lookupOrCreateSymbolIdAndUniqueIdForName(symbolName);
            ulong symbolId = symbolIdAndUniqueId.Item1;
            ulong uniqueId = symbolIdAndUniqueId.Item2;

            Pattern<AiThisAndThat.patternMatching.Decoration> symbolPattern = Pattern<AiThisAndThat.patternMatching.Decoration>.makeSymbol(symbolId, uniqueId);
            
            PatternManipulation.append(topPattern, symbolPattern);
        }


        void callbackEnterInstructionAndInstructionName(AiThisAndThat.lang.Parser parser, Token currentToken) {
            string calledFunctionName = currentToken.contentString;

            ulong uniqueId = patternSymbolContext.returnNewUniqueId();

            Pattern<AiThisAndThat.patternMatching.Decoration> instructionPattern = Pattern<AiThisAndThat.patternMatching.Decoration>.makeBranch(uniqueId);
            instructionPattern.decoration = new Decoration();
            instructionPattern.decoration.type = Decoration.EnumType.EXEC;
            instructionPattern.referenced = new Pattern<AiThisAndThat.patternMatching.Decoration>[1];

            ulong functionNamePatternUniqueId = patternSymbolContext.returnNewUniqueId();

            instructionPattern.referenced[0] = StringHelper.convert(calledFunctionName, functionNamePatternUniqueId);
            

            PatternManipulation.append(topPattern, instructionPattern);

            topPatternStack.Add(instructionPattern); // TODO< rewrite to push >
        }

        void callbackExitInstruction(AiThisAndThat.lang.Parser parser, Token currentToken) {
            topPatternStack.RemoveAt(topPatternStack.Count-1); // TODO< rewrite to pop >
        }

        void callbackInstructionParameterString(AiThisAndThat.lang.Parser parser, Token currentToken) {
            string stringContent = currentToken.contentString;

            ulong uniqueId = patternSymbolContext.returnNewUniqueId();
            Pattern<AiThisAndThat.patternMatching.Decoration> stringPattern = StringHelper.convert(stringContent, uniqueId);
            
            PatternManipulation.append(topPattern, stringPattern);
        }

        void callbackAddLong(AiThisAndThat.lang.Parser parser, Token currentToken) {
            ulong uniqueId = patternSymbolContext.returnNewUniqueId();

            Pattern<AiThisAndThat.patternMatching.Decoration> numberPattern = Pattern<AiThisAndThat.patternMatching.Decoration>.makeDecoratedValue(uniqueId);
            numberPattern.decoration = new Decoration();
            numberPattern.decoration.type = Decoration.EnumType.VALUE;
            numberPattern.decoration.value = (long)currentToken.contentNumber;

            PatternManipulation.append(topPattern, numberPattern);
        }

        void callbackAddString(AiThisAndThat.lang.Parser parser, Token currentToken) {
            string @string = currentToken.contentString;

            ulong stringUniqueId = patternSymbolContext.returnNewUniqueId();

            Pattern<Decoration> stringPattern = StringHelper.convert(@string, stringUniqueId);
            PatternManipulation.append(topPattern, stringPattern);
        }

        


        enum EnumRuleType {
            MATCH,
            TUPLE2,
            SEQUENCE,
            LOOP,
        }

        PatternSymbolContext patternSymbolContext;

        EnumRuleType? ruleType;

        IList< Pattern<AiThisAndThat.patternMatching.Decoration> > topPatternStack = new List< Pattern<AiThisAndThat.patternMatching.Decoration> >();
        
        public Pattern<AiThisAndThat.patternMatching.Decoration> rootPattern {
            get {
                Debug.Assert(topPatternStack.Count == 1); // must be the only element on stack else something while parsing gone wrong
                return topPatternStack[0];
            }
        }

        private Pattern<AiThisAndThat.patternMatching.Decoration> topPattern {
            get {
                return topPatternStack[topPatternStack.Count-1];
            }
        }
    }   
}