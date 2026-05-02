package com.gitvisualizer.network;

import com.fasterxml.jackson.databind.ObjectMapper;

import java.io.BufferedInputStream;
import java.io.BufferedOutputStream;
import java.io.IOException;
import java.net.Socket;
import java.nio.charset.StandardCharsets;

public final class CoopClient implements AutoCloseable {

    private final ObjectMapper mapper;
    private Socket socket;
    private BufferedOutputStream out;
    private BufferedInputStream in;

    public CoopClient(ObjectMapper mapper) {
        this.mapper = mapper;
    }

    public void connect(String host, int port) throws IOException {
        socket = new Socket(host, port);
        out = new BufferedOutputStream(socket.getOutputStream());
        in = new BufferedInputStream(socket.getInputStream());
    }

    public WireMessage readMessage() throws IOException {
        StringBuilder lineBuf = new StringBuilder();
        while (true) {
            int c = in.read();
            if (c < 0) {
                return null;
            }
            if (c == '\n') {
                String line = WireLineCodec.normalizeLine(lineBuf.toString());
                lineBuf.setLength(0);
                if (line.isEmpty()) {
                    continue;
                }
                return mapper.readValue(line, WireMessage.class);
            }
            lineBuf.append((char) c);
        }
    }

    public void sendResolve(int globalIndex) throws IOException {
        String json = mapper.writeValueAsString(WireMessage.resolve(globalIndex));
        synchronized (this) {
            out.write(json.getBytes(StandardCharsets.UTF_8));
            out.write('\n');
            out.flush();
        }
    }

    @Override
    public void close() throws IOException {
        if (socket != null && !socket.isClosed()) {
            socket.close();
        }
    }
}
