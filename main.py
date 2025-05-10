#!/usr/bin/env python3
import os
import argparse
import yaml
import pandas as pd
from jira_client import JiraClient
from data_processing import build_feature_table
from model import train_model, load_model, MODEL_PATH

# Pre-compute a reasonable Fibonacci sequence (up to, say, 100)
FIB_SEQUENCE = [1, 2, 3, 5, 8, 13, 21, 34, 55, 89]

def nearest_fibonacci(n: int) -> int:
    """
    Map integer n onto the closest Fibonacci number.
    If two fibs are equally close, pick the larger one.
    """
    # Compute absolute differences
    diffs = [(abs(n - f), f) for f in FIB_SEQUENCE]
    # Sort by (difference, -fib) so ties favor the larger fib
    _, fib = min(diffs, key=lambda x: (x[0], -x[1]))
    return fib

def parse_input(file_path):
    with open(file_path, 'r') as f:
        lines = [l.strip() for l in f if l.strip()]
    if lines[0].upper().startswith("JQL:"):
        return "jql", lines[0][4:].strip()
    else:
        return "keys", lines

def main():
    p = argparse.ArgumentParser(
        description="Smart Jira SP Estimator → Excel (Fibonacci‐mapped)"
    )
    p.add_argument("--input",  required=True,
                   help="Path to input.txt (JQL:… or list of keys)")
    p.add_argument("--train",  action="store_true",
                   help="Retrain model on this run")
    p.add_argument("--out",    default="output.xlsx",
                   help="Output Excel filename")
    args = p.parse_args()

    # --- Jira setup
    client = JiraClient("config.yaml")

    # --- Fetch issues
    mode, payload = parse_input(args.input)
    if mode == "jql":
        print(f"Fetching by JQL: {payload}")
        issues = client.fetch_by_jql(payload)
    else:
        print(f"Fetching by keys: {', '.join(payload)}")
        issues = client.fetch_by_keys(payload)

    if not issues:
        print("No issues found. Exiting.")
        return

    # --- Features + model
    df = build_feature_table(issues)
    if args.train or not os.path.exists(MODEL_PATH):
        print("Training model…")
        model = train_model(df)
    else:
        print("Loading existing model…")
        model = load_model()

    # --- Raw prediction
    # Drop only non-feature columns so pipeline gets everything it needs
    X_pred = df.drop(columns=["key", "orig_sp"])
    df["predicted_raw_sp"] = model.predict(X_pred).round().astype(int)

    # --- Fibonacci mapping
    df["New SP"] = df["predicted_raw_sp"].apply(nearest_fibonacci)

    # --- Output to Excel
    out_df = df[["key", "New SP"]].rename(columns={"key": "Issue Key"})
    out_df.to_excel(args.out, index=False)
    print(f"Wrote {len(out_df)} rows to {args.out}")

if __name__ == "__main__":
    main()
