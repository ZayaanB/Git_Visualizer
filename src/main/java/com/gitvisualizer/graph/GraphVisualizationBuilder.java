package com.gitvisualizer.graph;

import com.gitvisualizer.model.Branch;
import com.gitvisualizer.model.Commit;
import com.gitvisualizer.util.ColorUtils;

import java.time.OffsetDateTime;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeParseException;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Map;

/** Builds branch columns, commit positions along each branch, and stable branch colors from API data. */
public final class GraphVisualizationBuilder {

    public static final int COMMITS_PER_BRANCH = 50;
    public static final int MAX_BRANCHES_PER_PAGE = 100;

    private final double branchSpacing;
    private final double commitSpacing;

    public GraphVisualizationBuilder() {
        this(3.0, 1.5);
    }

    public GraphVisualizationBuilder(double branchSpacing, double commitSpacing) {
        this.branchSpacing = branchSpacing;
        this.commitSpacing = commitSpacing;
    }

    public GraphVisualizationPayload build(List<Branch> branches, Map<String, Commit[]> commitsByBranch) {
        if (branches == null || branches.isEmpty()) {
            return new GraphVisualizationPayload(List.of());
        }
        List<BranchVisualizationData> out = new ArrayList<>();
        for (int i = 0; i < branches.size(); i++) {
            Branch branch = branches.get(i);
            String branchName = branch != null && branch.name() != null ? branch.name() : "branch_" + i;
            Commit[] commits = commitsByBranch != null ? commitsByBranch.get(branchName) : null;
            if (commits == null || commits.length == 0) {
                continue;
            }
            List<Commit> sorted = new ArrayList<>(List.of(commits));
            sorted.sort(Comparator.comparing(GraphVisualizationBuilder::parseCommitDate));

            double[] branchColor = branchColorRgb(branchName);
            double xOffset = i * branchSpacing;

            List<GraphNodeData> nodes = new ArrayList<>();
            for (int j = 0; j < sorted.size(); j++) {
                Commit commit = sorted.get(j);
                String sha = commit.sha() != null ? commit.sha() : "";
                String msg = "";
                String author = "";
                String date = "";
                if (commit.commit() != null) {
                    if (commit.commit().message() != null) {
                        msg = truncate(commit.commit().message().trim(), 500);
                    }
                    if (commit.commit().author() != null && commit.commit().author().name() != null) {
                        author = truncate(commit.commit().author().name(), 120);
                    } else if (commit.author() != null && commit.author().login() != null) {
                        author = truncate(commit.author().login(), 120);
                    }
                    if (commit.commit().committer() != null && commit.commit().committer().date() != null) {
                        date = truncate(commit.commit().committer().date(), 60);
                    } else if (commit.commit().author() != null && commit.commit().author().date() != null) {
                        date = truncate(commit.commit().author().date(), 60);
                    }
                }
                nodes.add(new GraphNodeData(
                        xOffset,
                        0.0,
                        j * commitSpacing,
                        truncate(sha, 60),
                        msg,
                        author,
                        date,
                        j
                ));
            }
            out.add(new BranchVisualizationData(branchName, branchColor, nodes));
        }
        return new GraphVisualizationPayload(out);
    }

    static String truncate(String s, int maxChars) {
        if (s == null || s.isEmpty()) {
            return "";
        }
        return s.length() <= maxChars ? s : s.substring(0, maxChars);
    }

    static OffsetDateTime parseCommitDate(Commit commit) {
        String dateStr = null;
        if (commit.commit() != null) {
            if (commit.commit().committer() != null) {
                dateStr = commit.commit().committer().date();
            }
            if (dateStr == null && commit.commit().author() != null) {
                dateStr = commit.commit().author().date();
            }
        }
        if (dateStr == null || dateStr.isEmpty()) {
            return OffsetDateTime.MIN;
        }
        try {
            return OffsetDateTime.parse(dateStr, DateTimeFormatter.ISO_OFFSET_DATE_TIME);
        } catch (DateTimeParseException e) {
            try {
                return OffsetDateTime.parse(dateStr);
            } catch (DateTimeParseException e2) {
                return OffsetDateTime.MIN;
            }
        }
    }

    /** Hue derived from the branch name so the same branch always gets the same color. */
    public static double[] branchColorRgb(String branchName) {
        float hue = (Math.abs(branchName.hashCode()) % 360) / 360f;
        return ColorUtils.hsvToRgb(hue, 0.8, 0.9);
    }
}
