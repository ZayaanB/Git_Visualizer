package com.gitvisualizer.graph;

import com.gitvisualizer.model.Branch;
import com.gitvisualizer.model.BranchCommit;
import com.gitvisualizer.model.Commit;
import com.gitvisualizer.model.CommitInfo;
import com.gitvisualizer.model.GitAuthor;
import org.junit.jupiter.api.Test;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

class GraphVisualizationBuilderTest {

    @Test
    void buildsPayloadOrderedByDateAndAssignsIndices() {
        Branch main = new Branch("main", false, new BranchCommit("z", ""));
        Commit older = new Commit("a1", null, null, null,
                new CommitInfo(
                        new GitAuthor("x", null, "2020-01-01T00:00:00Z"),
                        new GitAuthor("x", null, "2020-01-01T00:00:00Z"),
                        "old",
                        null, null, 0
                ),
                null, null, null);
        Commit newer = new Commit("b2", null, null, null,
                new CommitInfo(
                        new GitAuthor("y", null, "2024-06-15T12:00:00Z"),
                        new GitAuthor("y", null, "2024-06-15T12:00:00Z"),
                        "new",
                        null, null, 0
                ),
                null, null, null);

        Map<String, Commit[]> byBranch = new HashMap<>();
        byBranch.put("main", new Commit[]{newer, older});

        GraphVisualizationBuilder builder = new GraphVisualizationBuilder(3.0, 1.5);
        GraphVisualizationPayload payload = builder.build(List.of(main), byBranch);

        assertEquals(1, payload.branchCount());
        assertEquals(2, payload.totalNodeCount());
        GraphNodeData n0 = payload.branches().get(0).nodes().get(0);
        GraphNodeData n1 = payload.branches().get(0).nodes().get(1);
        assertTrue(n0.message().contains("old"));
        assertTrue(n1.message().contains("new"));
        assertEquals(0.0, n0.x(), 1e-9);
        assertEquals(0, n0.indexInBranch());
        assertEquals(1.5, n1.z() - n0.z(), 1e-9);
    }

    @Test
    void branchColorIsDeterministic() {
        double[] c1 = GraphVisualizationBuilder.branchColorRgb("main");
        double[] c2 = GraphVisualizationBuilder.branchColorRgb("main");
        assertEquals(c1[0], c2[0], 1e-9);
        assertEquals(3, c1.length);
    }
}
