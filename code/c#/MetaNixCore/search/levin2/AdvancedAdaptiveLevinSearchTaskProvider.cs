﻿using System;
using System.Collections.Generic;

using MetaNix.scheduler;
using MetaNix.framework.logging;
using MetaNix.control.levinProgramSearch;
using System.Diagnostics;

namespace MetaNix.search.levin2 {
    // tries to supply the scheduler with new levin search tasks if it completed relevant tasks
    public class AdvancedAdaptiveLevinSearchTaskProvider : IObserver {
        // /param scheduler used to submit new tasks which search for the programs with levin search
        // /param sparseArrayProgramDistribution used distribution for choosing the instructions
        public AdvancedAdaptiveLevinSearchTaskProvider(
            Scheduler scheduler,
            SparseArrayProgramDistribution sparseArrayProgramDistribution,
            AdvancedAdaptiveLevinSearchProgramDatabase database,
            ILogger log
        ) {

            this.scheduler = scheduler;
            this.sparseArrayProgramDistribution = sparseArrayProgramDistribution;
            this.database = database;
            this.log = log;
        }



        // called from outside to submit the first task to the scheduler
        public void submitFirstTask() {
            tryToDequeueTaskAndSubmit();
        }

        // implementation of IObserver
        public void notify(params object[] arguments) {
            string messageType = (string)arguments[0];
            LevinSearchTask task = (LevinSearchTask)arguments[1];
            string taskName = (string)arguments[2];
            if (messageType == "success") {
                // a search was successful

                // TODO< retrive all tasks which solved the same problem with different parameters and stop them >

                tryToDequeueTaskAndSubmit();
            }
            else if (messageType == "failed") {
                // a search failed

                // we just suply the next task
                tryToDequeueTaskAndSubmit();
            }
        }

        // ignores if the # of ready problems is zero
        void tryToDequeueTaskAndSubmit() {
            if(problems.Count == 0) {
                return;
            }

            AdvancedAdaptiveLevinSearchProblem currentProblem = problems[0];
            problems.RemoveAt(0);
            submitTask(currentProblem);
        }

        void submitTask(AdvancedAdaptiveLevinSearchProblem problem) {
            
            Observable levinSearchObservable = new Observable();
            levinSearchObservable.register(new AdvancedAdaptiveLevinSearchLogObserver(log));
            levinSearchObservable.register(this); // register ourself to know when the task failed or succeeded

            LevinSearchTask levinSearchTask = new LevinSearchTask(levinSearchObservable, database, problem);
            

            if (problem.humanReadableIndirectlyCallableProgramNames.Count > 0) {
                // query the problem for the hint
                Debug.Assert(problem.humanReadableIndirectlyCallableProgramNames.Count == 1, "Implemented for just one subproblem");
                var q = database.getQuery().whereHumanReadableProgramName(problem.humanReadableIndirectlyCallableProgramNames[0]);

                foreach (var iq in q.enumerable) {
                    Debug.Assert(iq.program != null, "Problem must have been already solved to be added as an indrectly callable program!");

                    levinSearchTask.problem.indirectCallablePrograms.Add(iq);
                }
            }


            scheduler.addTaskSync(levinSearchTask);

            levinSearchTask.levinSearchContext = new LevinSearchContext(problem);
            levinSearchTask.levinSearchContext.localInterpreterState.maxNumberOfRetiredInstructions = problem.maxNumberOfRetiredInstructions;
            fillUsedInstructionSet(levinSearchTask.levinSearchContext);
            levinSearchTask.levinSearchContext.initiateSearch(sparseArrayProgramDistribution, problem.enumerationMaxProgramLength);
        }


        
        void fillUsedInstructionSet(LevinSearchContext levinSearchContext) {
            levinSearchContext.instructionIndexToInstruction.Clear();

            uint instructionIndex = 0;

            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = 0; // RET

            // add all nonspecial instructions
            for (uint instruction = 6; instruction <= 37; instruction++) {
                levinSearchContext.instructionIndexToInstruction[instructionIndex++] = instruction;
            }

            // add some special jump instructions

            // advance or exit
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(1, -4);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(1, -3);

            // not end or exit
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(2, -4);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(2, -5);

            // jump
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(3, -0); // nop
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(3, -2);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(3, -3);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(3, -4);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(3, -5);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(3, -6);
            // TODO< jump ahead >

            // jump if not flag
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(4, -2);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(4, -3);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(4, -4);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(4, -5);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(4, -6);

            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(4, +1);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(4, +2);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(4, +3);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(4, +4);
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = InstructionInterpreter.convInstructionAndRelativeToInstruction(4, +5);

            // call indirect
            levinSearchContext.instructionIndexToInstruction[instructionIndex++] = 42;

            int breakpointHere1 = 1;
        }


        public IList<AdvancedAdaptiveLevinSearchProblem> problems = new List<AdvancedAdaptiveLevinSearchProblem>();

        Scheduler scheduler;
        SparseArrayProgramDistribution sparseArrayProgramDistribution;
        private AdvancedAdaptiveLevinSearchProgramDatabase database;
        ILogger log;
    }

    // describes an problem which has to been solved with levinsearch
    public class AdvancedAdaptiveLevinSearchProblem {
        public AdvancedAdaptiveLevinSearchProblem shallowCopy() {
            AdvancedAdaptiveLevinSearchProblem copied = new AdvancedAdaptiveLevinSearchProblem();
            copied.globalInitialArrayState = globalInitialArrayState;
            //copied.localInitialInterpreterState = localInitialInterpreterState;
            copied.trainingSamples = trainingSamples;
            copied.enumerationMaxProgramLength = enumerationMaxProgramLength;
            copied.instructionsetCount = instructionsetCount;
            copied.maxNumberOfRetiredInstructions = maxNumberOfRetiredInstructions;
            copied.parentProgram = parentProgram;
            copied.humanReadableHints = humanReadableHints;
            copied.humanReadableIndirectlyCallableProgramNames = humanReadableIndirectlyCallableProgramNames;
            copied.humanReadableTaskname = humanReadableTaskname;
            return copied;
        }

        public AdvancedAdaptiveLevinSearchProblem setEnumerationMaxProgramLength(uint length) {
            enumerationMaxProgramLength = length;
            return this;
        }

        public AdvancedAdaptiveLevinSearchProblem setParentProgram(uint[] parentProgram) {
            this.parentProgram = parentProgram;
            return this;
        }

        public ArrayState globalInitialArrayState;
        //public LocalInterpreterState localInitialInterpreterState;
        public IList<TrainingSample> trainingSamples = new List<TrainingSample>();

        public string humanReadableTaskname = "?";

        public uint enumerationMaxProgramLength;

        public uint instructionsetCount;
        public uint maxNumberOfRetiredInstructions;

        // program which can be called by the current program
        public uint[] parentProgram; // can be null

        // solved problems with programs which are indirecly callable
        public IList<AdvancedAdaptiveLevinSearchProgramDatabaseEntry> indirectCallablePrograms = new List<AdvancedAdaptiveLevinSearchProgramDatabaseEntry>();

        public IList<string> humanReadableHints = new List<string>(); // are search hints which are/were used for searching related programs
                                                                      // programs with the same hints do approximatly solve the same problem

        // name of problems which can be called indirectly
        public IList<string> humanReadableIndirectlyCallableProgramNames = new List<string>();
    }
}
