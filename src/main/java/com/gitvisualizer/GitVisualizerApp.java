package com.gitvisualizer;

import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.gitvisualizer.graph.GraphVisualizationBuilder;
import com.gitvisualizer.graph.GraphVisualizationPayload;
import com.gitvisualizer.game.MergeConflictGame;
import com.gitvisualizer.network.CoopClient;
import com.gitvisualizer.network.CoopServer;
import com.gitvisualizer.network.GameSessionInit;
import com.gitvisualizer.network.WireMessage;
import com.gitvisualizer.service.GitHubService;
import com.gitvisualizer.ui.GameSessionPane;
import javafx.application.Application;
import javafx.application.Platform;
import javafx.concurrent.Task;
import javafx.geometry.Insets;
import javafx.geometry.Pos;
import javafx.scene.Scene;
import javafx.scene.control.Alert;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.control.PasswordField;
import javafx.scene.control.TextField;
import javafx.scene.layout.GridPane;
import javafx.scene.layout.VBox;
import javafx.stage.Stage;

import java.io.IOException;
import java.util.Arrays;

/** Desktop UI: menu, solo play, or LAN host/join on port 7777. */
public class GitVisualizerApp extends Application {

    private static final String DEFAULT_OWNER = "ZayaanB";
    private static final String DEFAULT_REPO = "Git_Visualizer";

    @Override
    public void start(Stage stage) {
        stage.setTitle("Git Visualizer");
        showMainMenu(stage);
        stage.show();
    }

    private void showMainMenu(Stage stage) {
        Label hint = new Label("GitHub token optional (use env GITHUB_TOKEN or field below).");
        hint.setWrapText(true);
        TextField owner = new TextField(DEFAULT_OWNER);
        TextField repo = new TextField(DEFAULT_REPO);
        PasswordField token = new PasswordField();
        String envTok = System.getenv("GITHUB_TOKEN");
        if (envTok != null && !envTok.isEmpty()) {
            token.setPromptText("(using GITHUB_TOKEN env)");
        }

        Button solo = new Button("Play Solo");
        solo.setPrefWidth(220);
        solo.setOnAction(e -> startSolo(stage, owner.getText(), repo.getText(), effectiveToken(token)));

        Button host = new Button("Co-op: Host (LAN)");
        host.setPrefWidth(220);
        host.setOnAction(e -> startCoopHost(stage, owner.getText(), repo.getText(), effectiveToken(token)));

        TextField joinIp = new TextField("127.0.0.1");
        joinIp.setPromptText("Host IP");
        Button join = new Button("Co-op: Join");
        join.setPrefWidth(220);
        join.setOnAction(e -> startCoopJoin(stage, joinIp.getText().trim()));

        Button exit = new Button("Exit");
        exit.setPrefWidth(220);
        exit.setOnAction(e -> Platform.exit());

        GridPane grid = new GridPane();
        grid.setHgap(8);
        grid.setVgap(8);
        grid.addRow(0, new Label("Owner"), owner);
        grid.addRow(1, new Label("Repo"), repo);
        grid.addRow(2, new Label("Token"), token);
        grid.addRow(3, new Label("Join IP"), joinIp);

        VBox root = new VBox(16, hint, grid, solo, host, join, exit);
        root.setPadding(new Insets(24));
        root.setAlignment(Pos.CENTER_LEFT);
        stage.setScene(new Scene(root, 520, 460));
    }

    private static String effectiveToken(PasswordField tokenField) {
        String env = System.getenv("GITHUB_TOKEN");
        if (env != null && !env.isBlank()) {
            return env.trim();
        }
        return tokenField.getText() != null ? tokenField.getText().trim() : "";
    }

    private void startSolo(Stage stage, String owner, String repo, String token) {
        Task<GraphVisualizationPayload> task = new Task<>() {
            @Override
            protected GraphVisualizationPayload call() throws Exception {
                GitHubService gh = new GitHubService();
                GitHubService.RepoDataResult data = gh.fetchRepoData(owner, repo, token);
                if (data == null) {
                    throw new IllegalStateException("Could not load repository (rate limit, 404, or network).");
                }
                GraphVisualizationBuilder builder = new GraphVisualizationBuilder();
                return builder.build(Arrays.asList(data.branches()), data.commitsByBranch());
            }
        };
        task.setOnSucceeded(ev -> {
            GraphVisualizationPayload payload = task.getValue();
            MergeConflictGame game = new MergeConflictGame();
            game.pickConflictsIfNeeded(payload.totalNodeCount());
            GameSessionPane pane = new GameSessionPane(payload, game, true, null, null, s -> showMainMenu(stage));
            stage.setScene(new Scene(pane, 1400, 800));
        });
        task.setOnFailed(ev -> Platform.runLater(() -> alert(Alert.AlertType.ERROR, task.getException() != null
                ? task.getException().getMessage()
                : "Load failed")));
        new Thread(task, "github-fetch").start();
    }

    private void startCoopHost(Stage stage, String owner, String repo, String token) {
        Label wait = new Label("Fetching repository, then listening for a client on port " + CoopServer.DEFAULT_PORT + "…");
        wait.setWrapText(true);
        VBox box = new VBox(12, wait);
        box.setPadding(new Insets(24));
        stage.setScene(new Scene(box, 560, 200));

        ObjectMapper mapper = wireMapper();
        Task<Void> task = new Task<>() {
            @Override
            protected Void call() throws Exception {
                GitHubService gh = new GitHubService();
                GitHubService.RepoDataResult data = gh.fetchRepoData(owner, repo, token);
                if (data == null) {
                    throw new IllegalStateException("Could not load repository.");
                }
                GraphVisualizationBuilder builder = new GraphVisualizationBuilder();
                GraphVisualizationPayload payload = builder.build(Arrays.asList(data.branches()), data.commitsByBranch());
                MergeConflictGame game = new MergeConflictGame();
                game.pickConflictsIfNeeded(payload.totalNodeCount());

                CoopServer server = new CoopServer(mapper, CoopServer.DEFAULT_PORT);
                server.start();
                Platform.runLater(() -> wait.setText("Waiting for client to connect on port " + CoopServer.DEFAULT_PORT + "…"));
                server.acceptClient();
                server.sendInit(payload, game);

                Platform.runLater(() -> {
                    GameSessionPane pane = new GameSessionPane(payload, game, true, server, null, s -> {
                        try {
                            server.close();
                        } catch (IOException ignored) {
                        }
                        showMainMenu(stage);
                    });
                    stage.setScene(new Scene(pane, 1400, 800));
                });

                Thread readThread = new Thread(() -> {
                    try {
                        server.runReadLoop(game);
                    } catch (IOException ignored) {
                    }
                }, "coop-host-read");
                readThread.setDaemon(true);
                readThread.start();
                return null;
            }
        };
        task.setOnFailed(ev -> Platform.runLater(() -> alert(Alert.AlertType.ERROR, task.getException() != null
                ? task.getException().getMessage()
                : "Host failed")));
        new Thread(task, "coop-host").start();
    }

    private void startCoopJoin(Stage stage, String hostIp) {
        if (hostIp == null || hostIp.isBlank()) {
            alert(Alert.AlertType.WARNING, "Enter the host IP address.");
            return;
        }
        Label wait = new Label("Connecting to " + hostIp + "…");
        stage.setScene(new Scene(new VBox(12, wait), 400, 120));

        ObjectMapper mapper = wireMapper();
        Task<Void> task = new Task<>() {
            @Override
            protected Void call() throws Exception {
                CoopClient client = new CoopClient(mapper);
                try {
                    client.connect(hostIp, CoopServer.DEFAULT_PORT);
                    WireMessage first = client.readMessage();
                    if (first == null || !"INIT".equals(first.kind()) || first.init() == null) {
                        throw new IllegalStateException("Did not receive INIT from host.");
                    }
                    GameSessionInit init = first.init();
                    GraphVisualizationPayload payload = init.graph();
                    if (payload == null) {
                        throw new IllegalStateException("Invalid graph payload.");
                    }
                    MergeConflictGame game = new MergeConflictGame();
                    game.applyNetworkSnapshot(
                            init.remainingTime(),
                            init.resolvedCount(),
                            init.conflictIndices() != null ? init.conflictIndices() : java.util.List.of(),
                            init.gameEnded(),
                            init.win()
                    );

                    CoopClient connected = client;
                    client = null;
                    Platform.runLater(() -> {
                        GameSessionPane pane = new GameSessionPane(payload, game, false, null, connected, s -> {
                            try {
                                connected.close();
                            } catch (IOException ignored) {
                            }
                            showMainMenu(stage);
                        });
                        stage.setScene(new Scene(pane, 1400, 800));
                    });
                    return null;
                } finally {
                    if (client != null) {
                        try {
                            client.close();
                        } catch (IOException ignored) {
                        }
                    }
                }
            }
        };
        task.setOnFailed(ev -> Platform.runLater(() -> alert(Alert.AlertType.ERROR, task.getException() != null
                ? task.getException().getMessage()
                : "Join failed")));
        new Thread(task, "coop-join").start();
    }

    private static ObjectMapper wireMapper() {
        return new ObjectMapper()
                .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
    }

    private static void alert(Alert.AlertType type, String msg) {
        Alert a = new Alert(type, msg);
        a.setHeaderText(null);
        a.showAndWait();
    }

    public static void main(String[] args) {
        launch(args);
    }
}
