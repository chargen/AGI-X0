﻿using System;
using System.Collections.Generic;

namespace MetaNix.dispatch {
    // dispatcher which tries to dispatch the calls to shadowable dispatchers. if no shadowable dispatcher handles a call then the call is passed to the proxy
    class ShadowableHiddenDispatcher : IHiddenDispatcher {
        IHiddenDispatcher proxy;

        // ordered by priority
        public IList<IShadowableDispatcher> shadowableDispatchers = new List<IShadowableDispatcher>();

        public ShadowableHiddenDispatcher(IHiddenDispatcher proxy) {
            this.proxy = proxy;
        }

        public Node dispatch(HiddenFunctionId hiddenFunctionId, IList<Node> arguments) {
            Node result;

            foreach (IShadowableDispatcher iDispatcher in shadowableDispatchers) {
                bool wasShadowed;
                result = iDispatcher.tryDispatch(hiddenFunctionId, arguments, out wasShadowed);
                if(wasShadowed) {
                    return result;
                }
            }

            result = proxy.dispatch(hiddenFunctionId, arguments);

            // inform all shadowable dispatchers about the result
            foreach(IShadowableDispatcher iDispatcher in shadowableDispatchers) {
                iDispatcher.informCompleteFallThroughWithResult(hiddenFunctionId, arguments, result);
            }

            return result;
        }
    }
}
