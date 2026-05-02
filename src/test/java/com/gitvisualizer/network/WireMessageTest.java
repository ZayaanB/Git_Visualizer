package com.gitvisualizer.network;

import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.gitvisualizer.graph.BranchVisualizationData;
import com.gitvisualizer.graph.GraphNodeData;
import com.gitvisualizer.graph.GraphVisualizationPayload;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;

class WireMessageTest {

    @Test
    void roundTripInit() throws Exception {
        ObjectMapper mapper = new ObjectMapper().configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
        GraphVisualizationPayload graph = new GraphVisualizationPayload(List.of(
                new BranchVisualizationData("main", new double[]{1, 0, 0}, List.of(
                        new GraphNodeData(0, 0, 0, "sha", "msg", "me", "2020-01-01", 0)
                ))
        ));
        GameSessionInit init = new GameSessionInit(graph, List.of(0, 2), 300f, 0, false, null);
        WireMessage out = WireMessage.init(init);
        String json = mapper.writeValueAsString(out);
        WireMessage in = mapper.readValue(json, WireMessage.class);
        assertEquals("INIT", in.kind());
        assertNotNull(in.init());
        assertEquals(1, in.init().graph().branchCount());
    }
}
