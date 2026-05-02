package com.gitvisualizer.network;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonInclude;

@JsonIgnoreProperties(ignoreUnknown = true)
@JsonInclude(JsonInclude.Include.NON_NULL)
public record WireMessage(
        String kind,
        Integer nodeIndex,
        GameSessionInit init,
        GameStateSnapshot state,
        String error
) {
    public static WireMessage resolve(int nodeIndex) {
        return new WireMessage("RESOLVE", nodeIndex, null, null, null);
    }

    public static WireMessage state(GameStateSnapshot snapshot) {
        return new WireMessage("STATE", null, null, snapshot, null);
    }

    public static WireMessage init(GameSessionInit init) {
        return new WireMessage("INIT", null, init, null, null);
    }
}
