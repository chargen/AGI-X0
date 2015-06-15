package ptrman.agix0.src.java.Evolvator;

import org.uncommons.watchmaker.framework.FitnessEvaluator;
import ptrman.agix0.src.java.Common.Component;
import ptrman.agix0.src.java.Common.SimulationContext;
import ptrman.agix0.src.java.UsageCases.CritterSimpleUsageCase;
import ptrman.agix0.src.java.UsageCases.IUsageCase;

import java.util.List;

import static java.lang.Float.max;


public class NetworkFitnessEvaluator implements FitnessEvaluator<NetworkGeneticExpression> {
    public NetworkFitnessEvaluator(SimulationContext simulationContext) {
        this.simulationContext = simulationContext;
    }

    @Override
    public double getFitness(NetworkGeneticExpression networkGeneticExpression, List<? extends NetworkGeneticExpression> list) {
        // last neurons are
        // [last] move forward
        // [last-1] rotate
        // [last-2] rotate



        final float CONNECTION_PENELIZE = 0.08f; // how much does a connection cost?
        final float NEURON_PENELIZE = 0.2f; // how much does a neuron cost?

        //final int numberOfInputNeurons = 1;





        float fitness = 5000.0f;

        // evaluate how many times the output neuron (neuron 0) got stimulated

        /*
        Neuroid<Float, Integer> neuroid = new Neuroid<>(new Neuroid.FloatWeighttypeHelper());
        neuroid.update = new Update(latencyAfterActivation, randomFiringPropability);

        neuroid.allocateNeurons(networkGeneticExpression.networkDescriptor.getNumberOfHiddenNeurons(), numberOfInputNeurons);
        neuroid.input = new boolean[numberOfInputNeurons];

        for( int neuronI = 0; neuronI < networkGeneticExpression.networkDescriptor.getNumberOfHiddenNeurons(); neuronI++ ) {
            neuroid.getGraph().neuronNodes[neuronI].graphElement.threshold = networkGeneticExpression.networkDescriptor.hiddenNeurons[neuronI].firingThreshold;
        }

        neuroid.addEdgeWeightTuples(networkGeneticExpression.networkDescriptor.connections);

        neuroid.initialize();
        */
        simulationContext.setComponent(new Component());

        simulationContext.getComponent().setupNeuroidNetwork(networkGeneticExpression.networkDescriptor);

        IUsageCase usageCase = new CritterSimpleUsageCase(simulationContext.environmentScriptingAccessor);

        final int numberOfNeuralSimulationSteps = usageCase.getNumberOfNeuralSimulationSteps();

        /*
        Environment environment = new Environment();
        environment.entities.add(new Entity());
        environment.entities.get(0).position = new ArrayRealVector(new double[]{0.0, 0.0});
        environment.entities.get(0).direction = new ArrayRealVector(new double[]{1.0, 0.0});
         */
        simulationContext.setupEnvironment();

        // simulate network
        // together with the environment
        for( int timestep = 0; timestep < numberOfNeuralSimulationSteps; timestep++ ) {
            // stimulate

            simulationContext.getComponent().setStimulus(usageCase.beforeNeuroidSimationStepGetNeuroidInputForNextStep(simulationContext.environment, timestep));
            simulationContext.modelTimestep();

            // read out result and rate

            final boolean[] neuronActivation = simulationContext.getComponent().getActiviationOfNeurons();

            usageCase.afterNeuroidSimulationStep(simulationContext.environment, neuronActivation);

            simulationContext.environment.timestep();
        }


        // reward for traveled distance
        fitness += simulationContext.environment.entities.get(0).body.body.getPosition().lengthSquared();

        //System.out.println(networkGeneticExpression.connectionsWithWeights.size());

        fitness -= ((float)networkGeneticExpression.networkDescriptor.connections.size() * CONNECTION_PENELIZE);

        fitness -= ((float)networkGeneticExpression.getEnabledNeurons() * NEURON_PENELIZE);

        //System.out.println(networkGeneticExpression.getEnabledNeurons());

        fitness = max(fitness, 0.0f);

        return fitness;
    }

    @Override
    public boolean isNatural() {
        return true;
    }

    private SimulationContext simulationContext;
}
