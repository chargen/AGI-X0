module slimRnn.codegen.CodegenVliw2;

// the instruction generator has a list of prefered instructions or instruction combinations
// and a list of simple instructions it can combine freely, with an priority for each instruction

// and it has a blacklist for instruction combinations

import std.format;
import std.string;
import std.algorithm.iteration : map, filter;
import std.algorithm.searching : all, canFind;
import std.conv : to;

void main() {
	import std.stdio;

	writeln("module autogenerated.AutogeneratedVliw2;");

	writeln("import memoryLowlevel.StackAllocator;");
	writeln("import slimRnn.SlimRnnStackBasedManipulationInstruction;");

	writeln(generateDCodeForTwoMacro());
}

string generateDCodeForTwoMacro() {
	string emitted;

	void emitLine(string line = "") {
		emitted ~= line ~ "\n";
	}

	emitLine("void vliw2emitInstructionsForTwoMacro(uint levinInstruction, uint delegate(uint index) translateTypeIndexOfPieceToType, ref StackAllocator!(8, SlimRnnStackBasedManipulationInstruction) resultInstructionStack, out bool invalidEncoding) {");
	emitLine(generateDCodeForTwoMacroInnerBody());
	emitLine("}");

	emitLine();
	emitLine();
	emitLine();

	emitLine("void vliw2emitInstructionsForOneMacro(uint levinInstruction, uint delegate(uint index) translateTypeIndexOfPieceToType, ref StackAllocator!(8, SlimRnnStackBasedManipulationInstruction) resultInstructionStack, out bool invalidEncoding) {");
	emitLine(generateDCodeForOneMacroInnerBody());
	emitLine("}");


	return emitted;
}

string generateDCodeForTwoMacroInnerBody() {
	string emitted;

	string data2Name = "INVALID"; // is invalid and will lead to an compiler error because we don't have an argument in the two macro function

	void emitLine(string line = "") {
		emitted ~= line ~ "\n";
	}

	emitLine("bool appendSuccess; // returned by the stack allocator, we throw it away");

	emitLine("invalidEncoding = false;");

	emitLine("uint macroIndex = levinInstruction & 0x3FF; // mask lowest 10 bits out");

	emitLine("switch(macroIndex) {");

	// two macro combinations use the data1 and data2 for macros
	// we generate code which decides the combination based on the combined index of data1 and data2

	const(EnumInstructionType)[][2][] twoMacroCombinations = generateTwoMacroCombinations();
	foreach( i, iterationTwoMacroCombination; twoMacroCombinations ) {
		emitLine("// emission for macro %s ||| %s".format(iterationTwoMacroCombination[0], iterationTwoMacroCombination[1]));
		emitLine("case %s:".format(i));

		emitLine("// left side");
		emitLine("%s;".format(generateDCodeForEmissionOfInstructions(iterationTwoMacroCombination[0], data2Name)));
		emitLine("// right side");
		emitLine("%s;".format(generateDCodeForEmissionOfInstructions(iterationTwoMacroCombination[1], data2Name)));

		emitLine("break;");
		emitLine("");
	}

	emitLine("default:");
	emitLine("invalidEncoding = true;"); // by default we reject the encoding

	emitLine("}");

	return emitted;
}

string generateDCodeForOneMacroInnerBody() {
	string emitted;

	string data2Name = "data2"; // is invalid and will lead to an compiler error because we don't have an argument in the two macro function

	void emitLine(string line) {
		emitted ~= line ~ "\n";
	}

	emitLine("bool appendSuccess; // returned by the stack allocator, we throw it away");

	emitLine("invalidEncoding = false;");

	emitLine("uint macroIndex = (levinInstruction >> 5) & 0x1f; // mask highest 5 bits out");
	emitLine("uint %s = levinInstruction & 0x1f; // mask lowest 5 bits out".format(data2Name));

	emitLine("switch(macroIndex) {");

	// one macro combinations use the data1 for macros, the rest for the data

	const(EnumInstructionType)[][] oneMacroCombinations = generateOneMacroCombinations();

	foreach( i, iterationOneMacroCombination; oneMacroCombinations ) {
		emitLine("// emission for macro %s".format(iterationOneMacroCombination));
		emitLine("case %s:".format(i));
		emitLine("%s;".format(generateDCodeForEmissionOfInstructions(iterationOneMacroCombination, data2Name)));
		emitLine("break;");
		emitLine("");
	}

	emitLine("default:");
	emitLine("invalidEncoding = true;"); // by default we reject the encoding

	emitLine("}");

	return emitted;
}

private string generateDCodeForEmissionOfInstructions(const(EnumInstructionType)[] instructions, string data2Name) {
	return instructions.map!(v => convertInstructionToEmissionCodeIntoArray(v, data2Name)).join(";\n");
}


private enum EnumInstructionType {
	LINKOUTPUTOFNEURONTOSLIMRNNOUTPUT0FORNEURONATSTACKTOP,	// must be first instruction because we will enumerate for an effective program length of 3 a program length 4, but we make sure that only the first 4th instruction gets checked
	LINKOUTPUTOFNEURONTOSLIMRNNOUTPUT1FORNEURONATSTACKTOP,

	ACTIVATENEURON,
	DEACTIVATENEURON,
	ACTIVATENEURONATSTACKTOP,
	DEACTIVATENEURONATSTACKTOP,
	COMMIT,

	DUPLICATESTACKTOP,        // pushes the top of the stack again
	POP,
	POP2,
	PUSHNEXTNEURONINDEX, // pushes the next neuron index
	PUSHCONSTANT, // requires a 5 bit argument
	SWAPSTACK, // swaps stack(top) and stack(top-1)

	SETOUTPUTHSTRENGTHTOVALUEFORNEURONATSTACKTOP, // set output strength of piece stack(top) to data    requires one 5 bit argument
	SETTYPEVARIABLEFORNEURONATSTACKTOP, // requires a 5 bit argument
	
	SETSWITCHBOARDINDEXFOROUTPUTFORPIECEATSTACKTOP,     // set output index of piece stack(top) to data       requires one 5 bit argument
	WIREINPUT0TONEURONINDEXFORNEURONATSTACKTOP, // requires a 5 bit argument
	WIREINPUT1TONEURONINDEXFORNEURONATSTACKTOP, // requires a 5 bit argument
	WIREINPUT2TONEURONINDEXFORNEURONATSTACKTOP, // requires a 5 bit argument

	WIREINPUT0TONEURONINDEX0FORNEURONATSTACKTOP,
	WIREINPUT1TONEURONINDEX1FORNEURONATSTACKTOP,
	WIREINPUT2TONEURONINDEX2FORNEURONATSTACKTOP,

	//PUTINTOWTAFORTOPNEURON, // requires 5 bit argument
	//                        // put neuron stack(top) into "winner takes all"-group data1


	NOP, // no operation performed

	RESETNEURONACTIVATIONFORNEURONATTOP,
}

private immutable EnumInstructionType[] allInstructions = [
	EnumInstructionType.LINKOUTPUTOFNEURONTOSLIMRNNOUTPUT0FORNEURONATSTACKTOP,
	EnumInstructionType.LINKOUTPUTOFNEURONTOSLIMRNNOUTPUT1FORNEURONATSTACKTOP,

	EnumInstructionType.ACTIVATENEURON,
	EnumInstructionType.DEACTIVATENEURON,
	EnumInstructionType.ACTIVATENEURONATSTACKTOP,
	EnumInstructionType.DEACTIVATENEURONATSTACKTOP,
	EnumInstructionType.COMMIT,

	EnumInstructionType.DUPLICATESTACKTOP,        // pushes the top of the stack again
	EnumInstructionType.POP,
	EnumInstructionType.POP2,
	EnumInstructionType.PUSHNEXTNEURONINDEX, // pushes the next neuron index
	EnumInstructionType.PUSHCONSTANT, // requires a 5 bit argument
	EnumInstructionType.SWAPSTACK, // swaps stack(top) and stack(top-1)

	EnumInstructionType.SETOUTPUTHSTRENGTHTOVALUEFORNEURONATSTACKTOP, // set output strength of piece stack(top) to data    requires one 5 bit argument
	EnumInstructionType.SETTYPEVARIABLEFORNEURONATSTACKTOP, // requires a 5 bit argument
	
	EnumInstructionType.SETSWITCHBOARDINDEXFOROUTPUTFORPIECEATSTACKTOP,     // set output index of piece stack(top) to data       requires one 5 bit argument
	EnumInstructionType.WIREINPUT0TONEURONINDEXFORNEURONATSTACKTOP, // requires a 5 bit argument
	EnumInstructionType.WIREINPUT1TONEURONINDEXFORNEURONATSTACKTOP, // requires a 5 bit argument
	EnumInstructionType.WIREINPUT2TONEURONINDEXFORNEURONATSTACKTOP, // requires a 5 bit argument

	EnumInstructionType.WIREINPUT0TONEURONINDEX0FORNEURONATSTACKTOP,
	EnumInstructionType.WIREINPUT1TONEURONINDEX1FORNEURONATSTACKTOP,
	EnumInstructionType.WIREINPUT2TONEURONINDEX2FORNEURONATSTACKTOP,

	//PUTINTOWTAFORTOPNEURON, // requires 5 bit argument
	//                        // put neuron stack(top) into "winner takes all"-group data1


	EnumInstructionType.NOP, // no operation performed

	EnumInstructionType.RESETNEURONACTIVATIONFORNEURONATTOP,
];

static assert(allInstructions.length != 0);
static assert(allInstructions.length <= 32); // has to fit into 5 bit!

private immutable bool[EnumInstructionType] instructionsRequiring5BitOperand;

static this() {
	with(EnumInstructionType) {
		instructionsRequiring5BitOperand = [
			PUSHCONSTANT: true,
			SETTYPEVARIABLEFORNEURONATSTACKTOP: true,
			SETOUTPUTHSTRENGTHTOVALUEFORNEURONATSTACKTOP: true,
			SETSWITCHBOARDINDEXFOROUTPUTFORPIECEATSTACKTOP: true,
			WIREINPUT0TONEURONINDEXFORNEURONATSTACKTOP: true,
			WIREINPUT1TONEURONINDEXFORNEURONATSTACKTOP: true,
			WIREINPUT2TONEURONINDEXFORNEURONATSTACKTOP: true,
			ACTIVATENEURON: true,
			DEACTIVATENEURON: true
		];
	}
}

private immutable bool[EnumInstructionType[2]] blacklistCombinedInstructions;

static this() {
	with(EnumInstructionType) {
		blacklistCombinedInstructions = [
			[ACTIVATENEURON, DEACTIVATENEURON]: true,
			[DEACTIVATENEURON, ACTIVATENEURON]: true,

			[ACTIVATENEURONATSTACKTOP, DEACTIVATENEURONATSTACKTOP]: true,
			[DEACTIVATENEURONATSTACKTOP, ACTIVATENEURONATSTACKTOP]: true,
			[RESETNEURONACTIVATIONFORNEURONATTOP, ACTIVATENEURONATSTACKTOP]: true, // doesn't make any sense

			// three pop is not required
			[POP, POP2]: true,
			[POP2, POP]: true,

			// doesn't make any sense
			[DUPLICATESTACKTOP, POP2]: true,
		];
	}
}

private EnumInstructionType[] instructionPriorities = [];

// contains the code which will be generated for the instruction
// the code gets later formated with the variablename of the arguments
private immutable string[EnumInstructionType] codeByInstructions;

static this() {
	with(EnumInstructionType) {
		codeByInstructions = [
			LINKOUTPUTOFNEURONTOSLIMRNNOUTPUT0FORNEURONATSTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeLinkOutputOfNeuronToSlimRnnOutputForNeuronAtStackTop(0)",
			LINKOUTPUTOFNEURONTOSLIMRNNOUTPUT1FORNEURONATSTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeLinkOutputOfNeuronToSlimRnnOutputForNeuronAtStackTop(1)",

			ACTIVATENEURON: "SlimRnnStackBasedManipulationInstruction.makeActivateNeuron(%s)",
			DEACTIVATENEURON: "SlimRnnStackBasedManipulationInstruction.makeDeactivateNeuron(%s)",
			ACTIVATENEURONATSTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeActivateNeuronAtStackTop()",
			DEACTIVATENEURONATSTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeDeactivateNeuronAtStackTop()",
			COMMIT: "SlimRnnStackBasedManipulationInstruction.makeCommit()",

			DUPLICATESTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeDuplicateStackTop()",
			POP: "SlimRnnStackBasedManipulationInstruction.makePop()",
			POP2: "SlimRnnStackBasedManipulationInstruction.makePop2()",
			PUSHNEXTNEURONINDEX: "SlimRnnStackBasedManipulationInstruction.makePushNextNeuronIndex()",
			PUSHCONSTANT: "SlimRnnStackBasedManipulationInstruction.makePushConstant(%s)",
			SWAPSTACK: "SlimRnnStackBasedManipulationInstruction.makeSwapStack()",
				

			SETOUTPUTHSTRENGTHTOVALUEFORNEURONATSTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeSetOutputStrengthForNeuronAtStackTop(cast(float)%s/cast(float)((1 << 5) - 1))",
			SETTYPEVARIABLEFORNEURONATSTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeSetTypeVariableForPieceAtStackTop(translateTypeIndexOfPieceToType(%s))", // we need to call the translation function because we need to translate index based to our CA rules, which are not equivalent to the index
			SETSWITCHBOARDINDEXFOROUTPUTFORPIECEATSTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeSetSwitchboardIndexForOutputForPieceAtStackTop(%s)",

			WIREINPUT0TONEURONINDEXFORNEURONATSTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeSetSwitchboardIndexForInputIndexAndPieceAtStackTop(0, %s)",
			WIREINPUT1TONEURONINDEXFORNEURONATSTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeSetSwitchboardIndexForInputIndexAndPieceAtStackTop(1, %s)",
			WIREINPUT2TONEURONINDEXFORNEURONATSTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeSetSwitchboardIndexForInputIndexAndPieceAtStackTop(2, %s)",

			WIREINPUT0TONEURONINDEX0FORNEURONATSTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeSetSwitchboardIndexForInputIndexAndPieceAtStackTop(0, 0)",
			WIREINPUT1TONEURONINDEX1FORNEURONATSTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeSetSwitchboardIndexForInputIndexAndPieceAtStackTop(1, 1)",
			WIREINPUT2TONEURONINDEX2FORNEURONATSTACKTOP: "SlimRnnStackBasedManipulationInstruction.makeSetSwitchboardIndexForInputIndexAndPieceAtStackTop(2, 2)",

			//PUTINTOWTAFORTOPNEURON, // requires 5 bit argument

			

			NOP: "SlimRnnStackBasedManipulationInstruction.makeNop()",
			RESETNEURONACTIVATIONFORNEURONATTOP: "SlimRnnStackBasedManipulationInstruction.makeResetNeuronActivationForNeuronAtTop()",
		];
	}
}

private string convertInstructionToEmissionCodeIntoArray(const(EnumInstructionType) instructionType, string data2Name) {
	return "resultInstructionStack.append(%s, /*out*/appendSuccess)".format(convertInstructionToEmissionCode(instructionType, data2Name));
}

// \param data2Name is the variablename of data2 which gets emitted into the code
private string convertInstructionToEmissionCode(const(EnumInstructionType) instructionType, string data2Name) {
	string template_ = codeByInstructions[instructionType];
	if( template_.canFind("%s") ) {
		return template_.format(data2Name);
	}
	return template_;
}

                                                           

// linear combinations of these are possible
// macro's can be made up of more than 2 instructions, they count as one value in the resulting instruction-set
private immutable EnumInstructionType[][] preferedMacros = [
	// must be the first because we enumerate effectivly n+1 instructions for an n long program, with this at the end
	[EnumInstructionType.LINKOUTPUTOFNEURONTOSLIMRNNOUTPUT0FORNEURONATSTACKTOP,],
	[EnumInstructionType.LINKOUTPUTOFNEURONTOSLIMRNNOUTPUT1FORNEURONATSTACKTOP,],

	// requires 5 bit operand
	[EnumInstructionType.ACTIVATENEURON,],
	[EnumInstructionType.DEACTIVATENEURON,],
	[EnumInstructionType.PUSHCONSTANT, EnumInstructionType.ACTIVATENEURON,],
	[EnumInstructionType.SETTYPEVARIABLEFORNEURONATSTACKTOP,], // we can't activate the neuron
	[EnumInstructionType.SETOUTPUTHSTRENGTHTOVALUEFORNEURONATSTACKTOP, EnumInstructionType.ACTIVATENEURON,],
	[EnumInstructionType.SETSWITCHBOARDINDEXFOROUTPUTFORPIECEATSTACKTOP, EnumInstructionType.ACTIVATENEURON,],

	// doesn't require operand
	[EnumInstructionType.PUSHNEXTNEURONINDEX, EnumInstructionType.ACTIVATENEURONATSTACKTOP,],

	[EnumInstructionType.ACTIVATENEURONATSTACKTOP, EnumInstructionType.WIREINPUT0TONEURONINDEX0FORNEURONATSTACKTOP, EnumInstructionType.WIREINPUT1TONEURONINDEX1FORNEURONATSTACKTOP,], // prefer this over the other version which pushes
	[EnumInstructionType.PUSHNEXTNEURONINDEX, EnumInstructionType.ACTIVATENEURONATSTACKTOP, EnumInstructionType.WIREINPUT0TONEURONINDEX0FORNEURONATSTACKTOP, EnumInstructionType.WIREINPUT1TONEURONINDEX1FORNEURONATSTACKTOP,],
	
	[EnumInstructionType.ACTIVATENEURONATSTACKTOP, EnumInstructionType.WIREINPUT0TONEURONINDEX0FORNEURONATSTACKTOP, EnumInstructionType.WIREINPUT1TONEURONINDEX1FORNEURONATSTACKTOP,],
];


// instruction format:

// prefix bits are two bits
// 00 : two MACROS without arguments
//      the next 10 bits are together one index into a big table (generated code)
// 01 : one macro with argument followed by argument
// 10 : unused
// 11 : push 10 bit value

// for the unused slots we could use other segmentations of macro and argument
// one slot could be filled with most combinations of instructions which are not expressible with prefix 00

private const(EnumInstructionType)[][2][] generateTwoMacroCombinations() {
	const(EnumInstructionType)[][2][] result;

	with( EnumInstructionType ) {
		// first we fill up all preferend macros (which don't need an operand) with NOP as the 2nd operation
		foreach( iterationPreferendMacro; preferedMacros.filter!(v => v.all!(b => !b.requires5BitOperand)) ) {
			result ~= [iterationPreferendMacro, [NOP]];
		}

		// now we add all instructions which don't require an operand
		// except NOP
		foreach( iterationSingleInstruction; allInstructions.filter!(b => !b.requires5BitOperand) ) {
			if( iterationSingleInstruction == NOP ) {
				continue;
			}

			result ~= [[iterationSingleInstruction], [NOP]];
		}

		// then we add all combinations of the prefered macros  and an instruction (for both sides)
		// we allow duplicates for the instructions on the left and right side and macro duplicates too

		//  prefered macros on the left side
		foreach( iterationPreferendMacro; preferedMacros.filter!(v => v.all!(b => !b.requires5BitOperand)) ) {
			foreach( iterationSingleInstruction; allInstructions.filter!(b => !b.requires5BitOperand) ) {
				// TODO< filter for blacklisted combined instructions >
				result ~= [iterationPreferendMacro, [iterationSingleInstruction]];
			}
		}

		//  prefered macros on the right side
		foreach( iterationPreferendMacro; preferedMacros.filter!(v => v.all!(b => !b.requires5BitOperand)) ) {
			foreach( iterationSingleInstruction; allInstructions.filter!(b => !b.requires5BitOperand) ) {
				// TODO< filter for blacklisted combined instructions >
				result ~= [[iterationSingleInstruction], iterationPreferendMacro];
			}
		}

		// TODO
	}

	return result;
}

private const(EnumInstructionType)[][] generateOneMacroCombinations() {
	const(EnumInstructionType)[][] result;
	
	with( EnumInstructionType ) {
		foreach( iterationSingleInstruction; allInstructions.filter!(b => b.requires5BitOperand) ) {
			result ~= [iterationSingleInstruction];
		}

		// combine the prefered macros which don't need an operand  with all instructions which need an operand
		// we add both directions
		foreach( iterationPreferendMacroWithoutOperand; preferedMacros.filter!(v => v.all!(b => !b.requires5BitOperand)) ) {
			foreach( iterationSingleInstruction; allInstructions.filter!(b => b.requires5BitOperand) ) {
				result ~= (iterationPreferendMacroWithoutOperand ~ iterationSingleInstruction);
				//result ~= (iterationSingleInstruction ~ iterationPreferendMacroWithoutOperand);
			}
		}

		/*
		// now we do the same for the prefered macros which need an operand
		foreach( iterationPreferendMacroWithoutOperand; preferedMacros.filter!(v => v.all!(b => b.requires5BitOperand)) ) {
			foreach( iterationSingleInstruction; allInstructions.filter!(b => !b.requires5BitOperand) ) {
				result ~= (iterationPreferendMacroWithoutOperand ~ iterationSingleInstruction);
				result ~= (iterationSingleInstruction ~ iterationPreferendMacroWithoutOperand);
			}
		}*/
	}

	return result;
}


// helper
private bool requires5BitOperand(const(EnumInstructionType) instruction) {
	return cast(bool)(instruction in instructionsRequiring5BitOperand);
}
