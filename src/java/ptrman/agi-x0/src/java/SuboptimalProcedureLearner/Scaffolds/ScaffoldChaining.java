package SuboptimalProcedureLearner.Scaffolds;

import Datastructures.Variadic;
import SuboptimalProcedureLearner.Scaffold;

import java.util.Arrays;
import java.util.List;

/**
 *
 * chains the Inner operator to the input
 * is mainly used for matching sequences.
 *
 * A B C D E F
 *
 * A B
 * \ /
 *  op
 *  |
 *  res0
 *
 *
 *  res0 C
 *   \  /
 *    op
 *    |
 *    res1
 *
 *       res1 D
 *         \ /
 *         op
 *          |
 *         res2
 *
 *  ...
 */
public class ScaffoldChaining extends Scaffold {
    @Override
    public String getShortName() {
        return this.getClass().getName();
    }

    @Override
    public ExecutionRequest executeScaffold() {

        Variadic leftSide;
        Variadic rightSide;

        if( resultSoFar == null ) {
            leftSide = allRemainingArguments.get(0);
            rightSide = allRemainingArguments.get(1);

            allRemainingArguments.remove(0);
            allRemainingArguments.remove(0);
        }
        else {
            leftSide = resultSoFar;
            rightSide = allRemainingArguments.get(0);

            allRemainingArguments.remove(0);
        }

        return new ExecutionRequest(internalOperatorIndex, Arrays.asList(leftSide, rightSide));
    }

    private int internalOperatorIndex;
    private List<Variadic> allRemainingArguments;

    private Variadic resultSoFar = null; // is null if it is the first call
}
