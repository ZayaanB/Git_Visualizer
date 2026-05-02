package com.gitvisualizer.util;

/** HSV (0–1 hue) to RGB for branch tinting. */
public final class ColorUtils {

    private ColorUtils() {
    }

    public static double[] hsvToRgb(double h, double s, double v) {
        double hh = (h % 1.0 + 1.0) % 1.0;
        double hh6 = hh * 6.0;
        int sector = (int) Math.floor(hh6);
        double f = hh6 - sector;
        double p = v * (1 - s);
        double q = v * (1 - f * s);
        double t = v * (1 - (1 - f) * s);

        double r;
        double g;
        double b;
        switch (sector % 6) {
            case 0 -> {
                r = v;
                g = t;
                b = p;
            }
            case 1 -> {
                r = q;
                g = v;
                b = p;
            }
            case 2 -> {
                r = p;
                g = v;
                b = t;
            }
            case 3 -> {
                r = p;
                g = q;
                b = v;
            }
            case 4 -> {
                r = t;
                g = p;
                b = v;
            }
            default -> {
                r = v;
                g = p;
                b = q;
            }
        }
        return new double[]{r, g, b};
    }
}
