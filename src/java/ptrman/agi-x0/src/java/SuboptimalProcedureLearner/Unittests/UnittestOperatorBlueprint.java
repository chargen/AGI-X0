package SuboptimalProcedureLearner.Unittests;

import SuboptimalProcedureLearner.OperatorBlueprint;
import SuboptimalProcedureLearner.Operators.OperatorAdd;
import SuboptimalProcedureLearner.Operators.OperatorConstant;
import ptrman.misc.Assert;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public class UnittestOperatorBlueprint {
    public static void unittest() {
        test();
    }

    private static void test() {
        OperatorBlueprint planOrginal;
        OperatorBlueprint.TreeElement rootElementOrginal;
        rootElementOrginal = new OperatorBlueprint.TreeElement(OperatorBlueprint.TreeElement.EnumType.BRANCH);
        rootElementOrginal.operatorToInstanciate = new OperatorAdd();
        rootElementOrginal.childrens = new ArrayList<>();
        rootElementOrginal.childrens.add(new OperatorBlueprint.TreeElement(OperatorBlueprint.TreeElement.EnumType.DUMMY));
        rootElementOrginal.childrens.add(new OperatorBlueprint.TreeElement(OperatorBlueprint.TreeElement.EnumType.DUMMY));
        rootElementOrginal.childrens.get(0).apearsInParentParameterInstances = true;
        rootElementOrginal.childrens.get(1).apearsInParentParameterInstances = true;
        List<List<Integer>> pathsToFreeOperatorsForAdd = new ArrayList<>();
        pathsToFreeOperatorsForAdd.add(new ArrayList<Integer>(Arrays.asList(0)));
        pathsToFreeOperatorsForAdd.add(new ArrayList<Integer>(Arrays.asList(1)));
        planOrginal = OperatorBlueprint.createFromTree(rootElementOrginal, pathsToFreeOperatorsForAdd);
        OperatorBlueprint.TreeElement treeElementConstantA;
        treeElementConstantA = new OperatorBlueprint.TreeElement(OperatorBlueprint.TreeElement.EnumType.LEAF);
        treeElementConstantA.operatorToInstanciate = new OperatorConstant();
        treeElementConstantA.constantsForOperatorInstance = new Datastructures.Variadic[1];
        treeElementConstantA.constantsForOperatorInstance[0] = new Datastructures.Variadic(Datastructures.Variadic.EnumType.INT);
        treeElementConstantA.constantsForOperatorInstance[0].valueInt = 1;
        OperatorBlueprint constantABlueprint = OperatorBlueprint.createFromTree(treeElementConstantA, new ArrayList<>());
        OperatorBlueprint.TreeElement treeElementConstantB;
        treeElementConstantB = new OperatorBlueprint.TreeElement(OperatorBlueprint.TreeElement.EnumType.LEAF);
        treeElementConstantB.operatorToInstanciate = new OperatorConstant();
        treeElementConstantB.constantsForOperatorInstance = new Datastructures.Variadic[1];
        treeElementConstantB.constantsForOperatorInstance[0] = new Datastructures.Variadic(Datastructures.Variadic.EnumType.INT);
        treeElementConstantB.constantsForOperatorInstance[0].valueInt = 2;
        OperatorBlueprint constantBBlueprint = OperatorBlueprint.createFromTree(treeElementConstantB, new ArrayList<>());
        // merge them (with replace)
        OperatorBlueprint compositionTemp = OperatorBlueprint.compose(planOrginal, new ArrayList<>(Arrays.asList(0)), constantABlueprint, OperatorBlueprint.EnumReplace.YES);
        OperatorBlueprint composition = OperatorBlueprint.compose(compositionTemp, new ArrayList<>(Arrays.asList(1)), constantBBlueprint, OperatorBlueprint.EnumReplace.YES);
        // check if result blueprint has any dummies
        Assert.Assert(OperatorBlueprint.countDummiesRecursive(composition) == 0, "");
    }
}