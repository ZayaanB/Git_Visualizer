package com.gitvisualizer.ui;

import com.gitvisualizer.game.MergeConflictGame;
import com.gitvisualizer.graph.GraphVisualizationPayload;
import com.gitvisualizer.network.CoopClient;
import com.gitvisualizer.network.CoopServer;
import com.gitvisualizer.network.WireMessage;
import javafx.animation.AnimationTimer;
import javafx.application.Platform;
import javafx.geometry.Insets;
import javafx.geometry.Pos;
import javafx.scene.*;
import javafx.scene.input.PickResult;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.input.KeyCode;
import javafx.scene.input.KeyEvent;
import javafx.scene.input.MouseButton;
import javafx.scene.input.ScrollEvent;
import javafx.scene.layout.BorderPane;
import javafx.scene.layout.HBox;
import javafx.scene.layout.StackPane;
import javafx.scene.layout.VBox;
import javafx.scene.paint.Color;
import javafx.scene.paint.PhongMaterial;
import javafx.scene.shape.Sphere;
import javafx.scene.transform.Rotate;
import javafx.scene.transform.Translate;

import java.util.HashMap;
import java.util.Map;
import java.util.function.Consumer;

/** Main play screen: 3D graph, HUD, hold-to-resolve; host pushes state to clients over TCP. */
public class GameSessionPane extends BorderPane {

    private final MergeConflictGame game;
    private final boolean authoritativeTimer;
    private final CoopServer coopServer;
    private final CoopClient coopClient;
    private final Consumer<String> goMainMenu;
    private final Graph3DFactory.GraphMeshes meshes;

    private final Label timerLabel = new Label();
    private final Label progressLabel = new Label();
    private final VBox endOverlay = new VBox(12);
    private final Label endTitle = new Label();
    private final Map<Integer, Sphere> indexToSphere = new HashMap<>();
    private final Map<Integer, Color> indexToBranchColor = new HashMap<>();

    private final Group world = new Group();
    private final PerspectiveCamera camera = new PerspectiveCamera(true);

    private double anchorX;
    private double anchorY;
    private double anchorAngleX;
    private double anchorAngleY;
    private double angleX = -18;
    private double angleY = 28;
    private double panX;
    private double panZ;
    private double distance = 18;

    private boolean orbiting;
    private long holdStartNanos;
    private Sphere holdingSphere;

    private float prevDisplaySec = MergeConflictGame.TIMER_DURATION_SEC;
    private boolean endOverlayShown;
    // Host: send first STATE immediately after connect, then every ~250 ms.
    private long lastNetworkSendNanos = -1L;

    private boolean keyW;
    private boolean keyA;
    private boolean keyS;
    private boolean keyD;

    public GameSessionPane(
            GraphVisualizationPayload payload,
            MergeConflictGame game,
            boolean authoritativeTimer,
            CoopServer coopServer,
            CoopClient coopClient,
            Consumer<String> goMainMenu) {
        this.game = game;
        this.authoritativeTimer = authoritativeTimer;
        this.coopServer = coopServer;
        this.coopClient = coopClient;
        this.goMainMenu = goMainMenu;
        this.meshes = Graph3DFactory.build(payload);

        setStyle("-fx-background-color: #0d1520;");

        timerLabel.setStyle("-fx-text-fill: white; -fx-font-size: 28px;");
        progressLabel.setStyle("-fx-text-fill: #cfefff; -fx-font-size: 16px;");
        HBox top = new HBox(24, timerLabel, progressLabel);
        top.setPadding(new Insets(12));
        top.setAlignment(Pos.CENTER_LEFT);
        setTop(top);

        for (Graph3DFactory.IndexedSphere is : meshes.spheres()) {
            indexToSphere.put(is.globalIndex(), is.sphere());
            indexToBranchColor.put(is.globalIndex(), is.branchColor());
        }

        world.getChildren().add(meshes.root());

        camera.setNearClip(0.1);
        camera.setFarClip(5000);
        Group cameraHolder = new Group();
        cameraHolder.getChildren().add(camera);
        camera.getTransforms().add(new Translate(0, 0, -distance));

        Group pivot = new Group();
        pivot.getChildren().addAll(world, cameraHolder);

        SubScene sub = new SubScene(pivot, 1200, 700, true, SceneAntialiasing.BALANCED);
        sub.setFill(Color.rgb(5, 12, 20));
        sub.setCamera(camera);

        StackPane centerStack = new StackPane(sub);
        setCenter(centerStack);

        Label msg = new Label("-");
        Label author = new Label("-");
        Label date = new Label("-");
        msg.setWrapText(true);
        msg.setStyle("-fx-text-fill: white; -fx-font-size: 14px;");
        author.setStyle("-fx-text-fill: #cdefff;");
        date.setStyle("-fx-text-fill: #cdefff;");
        VBox details = new VBox(8, new Label("Commit"), msg, author, date);
        details.setPadding(new Insets(16));
        details.setPrefWidth(280);
        details.setStyle("-fx-background-color: rgba(20,28,40,0.92);");
        setRight(details);

        Button menu = new Button("Main Menu");
        menu.setOnAction(e -> goMainMenu.accept(null));
        top.getChildren().add(menu);

        endOverlay.setAlignment(Pos.CENTER);
        endOverlay.setStyle("-fx-background-color: rgba(10,15,25,0.88);");
        endOverlay.setVisible(false);
        endTitle.setStyle("-fx-text-fill: white; -fx-font-size: 36px;");
        Button ok = new Button("OK");
        ok.setOnAction(e -> endOverlay.setVisible(false));
        endOverlay.getChildren().addAll(endTitle, ok);
        centerStack.getChildren().add(endOverlay);

        sub.setOnMousePressed(e -> {
            sub.requestFocus();
            if (e.getButton() == MouseButton.SECONDARY) {
                orbiting = true;
                anchorX = e.getSceneX();
                anchorY = e.getSceneY();
                anchorAngleX = angleX;
                anchorAngleY = angleY;
            } else if (e.getButton() == MouseButton.PRIMARY) {
                PickResult pr = e.getPickResult();
                if (pr != null && pr.getIntersectedNode() instanceof Sphere sp) {
                    Integer idx = findIndex(sp);
                    if (idx != null && game.isConflictNode(idx) && !game.gameEnded()) {
                        holdingSphere = sp;
                        holdStartNanos = System.nanoTime();
                    } else if (idx != null) {
                        var node = meshes.spheres().stream()
                                .filter(x -> x.globalIndex() == idx)
                                .findFirst()
                                .map(Graph3DFactory.IndexedSphere::data)
                                .orElse(null);
                        var c = CommitDetailsFormatter.fromNodeData(node);
                        msg.setText(CommitDetailsFormatter.message(c));
                        author.setText(CommitDetailsFormatter.author(c));
                        date.setText(CommitDetailsFormatter.date(c));
                    }
                }
            }
        });

        sub.setOnMouseDragged(e -> {
            if (orbiting && e.getButton() == MouseButton.SECONDARY) {
                angleX = anchorAngleX + (e.getSceneY() - anchorY) * 0.35;
                angleY = anchorAngleY + (e.getSceneX() - anchorX) * 0.35;
                angleX = Math.max(-89, Math.min(89, angleX));
                applyWorldTransform();
            }
        });

        sub.setOnMouseReleased(e -> {
            if (e.getButton() == MouseButton.SECONDARY) {
                orbiting = false;
            } else if (e.getButton() == MouseButton.PRIMARY) {
                holdingSphere = null;
            }
        });

        sub.addEventHandler(ScrollEvent.SCROLL, e -> {
            double delta = e.getDeltaY();
            distance -= delta * 0.04;
            distance = Math.max(2, Math.min(120, distance));
            camera.getTransforms().clear();
            camera.getTransforms().add(new Translate(0, 0, -distance));
            e.consume();
        });

        sceneProperty().addListener((o, a, s) -> {
            if (s != null) {
                installKeys(s);
            }
        });

        applyWorldTransform();

        AnimationTimer timer = new AnimationTimer() {
            private long last = -1;

            @Override
            public void handle(long now) {
                if (last < 0) {
                    last = now;
                    return;
                }
                float dt = (now - last) / 1_000_000_000f;
                last = now;

                double yawRad = Math.toRadians(angleY);
                double speed = 8.0 * dt;
                double fx = 0;
                double fz = 0;
                if (keyW) {
                    fz -= 1;
                }
                if (keyS) {
                    fz += 1;
                }
                if (keyA) {
                    fx -= 1;
                }
                if (keyD) {
                    fx += 1;
                }
                if (fx != 0 || fz != 0) {
                    panX += (fx * Math.cos(yawRad) + fz * Math.sin(yawRad)) * speed;
                    panZ += (-fx * Math.sin(yawRad) + fz * Math.cos(yawRad)) * speed;
                    applyWorldTransform();
                }

                if (holdingSphere != null) {
                    Integer hi = findIndex(holdingSphere);
                    if (hi == null || game.gameEnded() || !game.isConflictNode(hi)) {
                        holdingSphere = null;
                    } else {
                        long held = System.nanoTime() - holdStartNanos;
                        if (held >= (long) (MergeConflictGame.RESOLVE_HOLD_SEC * 1_000_000_000L)) {
                            holdingSphere = null;
                            tryResolve(hi);
                        }
                    }
                }

                if (authoritativeTimer && !game.gameEnded()) {
                    game.tick(dt);
                }

                float displaySec = (float) Math.floor(game.remainingTime());
                game.warningShouldPlay(prevDisplaySec, displaySec);
                prevDisplaySec = displaySec;

                updateHud();

                if (coopServer != null && authoritativeTimer
                        && (lastNetworkSendNanos < 0L || now - lastNetworkSendNanos > 250_000_000L)) {
                    lastNetworkSendNanos = now;
                    try {
                        coopServer.sendState(game);
                    } catch (Exception ignored) {
                    }
                }
            }
        };
        timer.start();

        if (coopClient != null) {
            Thread net = new Thread(() -> {
                try {
                    while (!Thread.currentThread().isInterrupted()) {
                        WireMessage wire = coopClient.readMessage();
                        if (wire == null) {
                            break;
                        }
                        if ("STATE".equals(wire.kind()) && wire.state() != null) {
                            var s = wire.state();
                            Platform.runLater(() -> {
                                game.applyNetworkSnapshot(
                                        s.remainingTime(),
                                        s.resolvedCount(),
                                        s.conflictIndices(),
                                        s.gameEnded(),
                                        s.win()
                                );
                                refreshSphereMaterials();
                                updateHud();
                            });
                        }
                    }
                } catch (Exception ignored) {
                }
            }, "coop-client-read");
            net.setDaemon(true);
            net.start();
        }

        refreshSphereMaterials();
        updateHud();
    }

    private void installKeys(Scene scene) {
        scene.addEventFilter(KeyEvent.KEY_PRESSED, e -> {
            KeyCode c = e.getCode();
            if (c == KeyCode.W) {
                keyW = true;
            }
            if (c == KeyCode.A) {
                keyA = true;
            }
            if (c == KeyCode.S) {
                keyS = true;
            }
            if (c == KeyCode.D) {
                keyD = true;
            }
        });
        scene.addEventFilter(KeyEvent.KEY_RELEASED, e -> {
            KeyCode c = e.getCode();
            if (c == KeyCode.W) {
                keyW = false;
            }
            if (c == KeyCode.A) {
                keyA = false;
            }
            if (c == KeyCode.S) {
                keyS = false;
            }
            if (c == KeyCode.D) {
                keyD = false;
            }
        });
    }

    private void tryResolve(int idx) {
        if (game.gameEnded()) {
            return;
        }
        if (coopClient != null) {
            // Joining player: host applies the resolve; UI follows incoming STATE messages.
            try {
                coopClient.sendResolve(idx);
            } catch (Exception ignored) {
            }
            return;
        }
        game.resolveConflict(idx);
        refreshSphereMaterials();
        updateHud();
    }

    private void applyWorldTransform() {
        world.getTransforms().setAll(
                new Rotate(angleX, Rotate.X_AXIS),
                new Rotate(angleY, Rotate.Y_AXIS),
                new Translate(panX, 0, panZ)
        );
    }

    private Integer findIndex(Sphere sp) {
        for (var e : indexToSphere.entrySet()) {
            if (e.getValue() == sp) {
                return e.getKey();
            }
        }
        return null;
    }

    private void updateHud() {
        int m = (int) (game.remainingTime() / 60);
        int s = (int) (game.remainingTime() % 60);
        timerLabel.setText(String.format("%02d:%02d", m, s));
        progressLabel.setText(String.format("Conflicts: %d/%d", game.resolvedCount(), MergeConflictGame.CONFLICT_COUNT));
        if (game.gameEnded() && !endOverlayShown) {
            endOverlayShown = true;
            endTitle.setText(game.win() ? "YOU WIN!" : "GAME OVER");
            endOverlay.setVisible(true);
        }
        refreshSphereMaterials();
    }

    private void refreshSphereMaterials() {
        for (var e : indexToSphere.entrySet()) {
            int idx = e.getKey();
            Sphere sp = e.getValue();
            Color base = indexToBranchColor.getOrDefault(idx, Color.LIGHTSTEELBLUE);
            boolean conflict = game.isConflictNode(idx);
            Color diffuse = conflict ? Color.color(1, 0.25, 0.2) : base;
            PhongMaterial mat = new PhongMaterial(diffuse);
            sp.setMaterial(mat);
        }
    }
}
