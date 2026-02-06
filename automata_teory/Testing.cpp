#include <iostream>
#include <string>
using namespace std;

class Automaton {
private:
    int state;

public:
    Automaton() : state(0) {}

    char process(char input) {
        char output;
        if (input == 'a') {
            if (state == 0) { state = 1; output = 'x'; }
            else if (state == 1) { state = 2; output = 'y'; }
            else { state = 0; output = 'z'; }
        }
        else if (input == 'b') {
            if (state == 0) { state = 2; output = 'y'; }
            else if (state == 1) { state = 0; output = 'z'; }
            else { state = 1; output = 'x'; }
        }
        else if (input == 'c') {
            if (state == 0) { output = 'z'; }
            else if (state == 1) { output = 'x'; }
            else { output = 'y'; }
        }
        else {
            return '\0';
        }
        return output;
    }
};

int main() {
    Automaton automaton;
    string line;

    while (getline(cin, line)) {
        automaton = Automaton();
        string output;

        for (char c : line) {
            char result = automaton.process(c);
            if (result != '\0') {
                output += result;
            }
        }

        cout << output << endl;
    }

    return 0;
}
