#!/bin/bash

# Session name
SESSION_NAME="dev-session"

# Start a new tmux session, but don't attach
tmux new-session -d -s $SESSION_NAME

# Rename the first window and run task:quick-dev
tmux rename-window -t $SESSION_NAME:0 "quick-dev"
tmux send-keys -t $SESSION_NAME:0 "task quick-dev" C-m

# Create a second window for k9s
tmux new-window -t $SESSION_NAME -n "k9s"
tmux send-keys -t $SESSION_NAME:1 "k9s" C-m

# Attach to the tmux session
tmux attach -t $SESSION_NAME
