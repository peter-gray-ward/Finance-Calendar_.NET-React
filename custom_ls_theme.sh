#!/bin/bash

# Enable LS_COLORS for file type coloring
export LS_COLORS="\
di=1;34:\
ln=1;36:\
so=35:\
pi=33:\
ex=1;32:\
bd=1;33;40:\
cd=1;33;40:\
su=0;41:\
sg=0;46:\
tw=30;42:\
ow=34;43:\
or=31:\
mi=5;37;41"

# Alias 'ls' to always show color
if [[ "$OSTYPE" == "darwin"* ]]; then
  alias ls="ls -G"   # macOS
else
  alias ls="ls --color=auto"  # Linux
fi

# Optional: load ~/.dircolors if it exists
if [ -f ~/.dircolors ]; then
  eval "$(dircolors -b ~/.dircolors)"
fi

# Print the current directory in bold
function print-bold-pwd() {
  echo -e "\n\e[1m📁 Current Directory: $(pwd)\e[0m\n"
}

# Show LS_COLORS breakdown
function show-ls-colors() {
  echo -e "Current LS_COLORS:\n$LS_COLORS" | tr ':' '\n'
}

print-bold-pwd
echo -e "\e[1;32m[✓] Custom LS_COLORS applied.\e[0m"

autoload -Uz colors && colors

PROMPT='%n@%m %F{blue}%B%U%~%u%b%f %# '
