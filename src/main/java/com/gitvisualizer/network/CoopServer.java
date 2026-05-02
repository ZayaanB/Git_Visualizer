package com.gitvisualizer.network;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.gitvisualizer.game.MergeConflictGame;
import com.gitvisualizer.graph.GraphVisualizationPayload;

import java.io.BufferedInputStream;
import java.io.BufferedOutputStream;
import java.io.IOException;
import java.net.ServerSocket;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.atomic.AtomicBoolean;

/** Host listens on one socket; one joiner gets INIT then STATE lines; RESOLVE lines come back from the client. */
public final class CoopServer implements AutoCloseable {

    public static final int DEFAULT_PORT = 7777;

    private final int port;
    private final ObjectMapper mapper;
    private ServerSocket serverSocket;
    private Socket clientSocket;
    private BufferedOutputStream clientOut;
    private final AtomicBoolean running = new AtomicBoolean(true);

    public CoopServer(ObjectMapper mapper, int port) {
        this.mapper = mapper;
        this.port = port;
    }

    public void start() throws IOException {
        serverSocket = new ServerSocket(port);
    }

    /** Blocks until a client connects. */
    public void acceptClient() throws IOException {
        clientSocket = serverSocket.accept();
        clientOut = new BufferedOutputStream(clientSocket.getOutputStream());
    }

    public void sendInit(GraphVisualizationPayload graph, MergeConflictGame game) throws IOException {
        List<Integer> conflicts = new ArrayList<>(game.conflictIndices());
        GameSessionInit init = new GameSessionInit(
                graph,
                conflicts,
                game.remainingTime(),
                game.resolvedCount(),
                game.gameEnded(),
                game.gameEnded() ? game.win() : null
        );
        writeLine(mapper.writeValueAsString(WireMessage.init(init)));
    }

    public void sendState(MergeConflictGame game) throws IOException {
        List<Integer> conflicts = new ArrayList<>(game.conflictIndices());
        Boolean win = game.gameEnded() ? game.win() : null;
        GameStateSnapshot snap = new GameStateSnapshot(
                game.remainingTime(),
                game.resolvedCount(),
                conflicts,
                game.gameEnded(),
                win
        );
        writeLine(mapper.writeValueAsString(WireMessage.state(snap)));
    }

    private void writeLine(String json) throws IOException {
        byte[] line = json.getBytes(StandardCharsets.UTF_8);
        synchronized (this) {
            if (clientOut == null) {
                return;
            }
            clientOut.write(line);
            clientOut.write('\n');
            clientOut.flush();
        }
    }

    /**
     * Processes incoming RESOLVE messages from the client on the calling thread (blocking read).
     */
    public void runReadLoop(MergeConflictGame game) throws IOException {
        BufferedInputStream in = new BufferedInputStream(clientSocket.getInputStream());
        StringBuilder lineBuf = new StringBuilder();
        while (running.get()) {
            int c = in.read();
            if (c < 0) {
                break;
            }
            if (c == '\n') {
                String line = WireLineCodec.normalizeLine(lineBuf.toString());
                lineBuf.setLength(0);
                if (line.isEmpty()) {
                    continue;
                }
                WireMessage msg = mapper.readValue(line, WireMessage.class);
                if ("RESOLVE".equals(msg.kind()) && msg.nodeIndex() != null) {
                    game.resolveConflict(msg.nodeIndex());
                }
            } else {
                lineBuf.append((char) c);
            }
        }
    }

    public void stopAccept() {
        running.set(false);
        try {
            if (serverSocket != null && !serverSocket.isClosed()) {
                serverSocket.close();
            }
        } catch (IOException ignored) {
        }
    }

    @Override
    public void close() throws IOException {
        running.set(false);
        if (clientSocket != null && !clientSocket.isClosed()) {
            clientSocket.close();
        }
        if (serverSocket != null && !serverSocket.isClosed()) {
            serverSocket.close();
        }
    }
}
