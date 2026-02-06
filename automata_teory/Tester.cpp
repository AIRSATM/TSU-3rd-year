#include <iostream>
#include <fstream>
#include <string>
#include <vector>
using namespace std;

struct TestResult {
    int testNumber;
    string input;
    string expected;
    string actual;
    bool passed;
    vector<int> errorPositions;
};

vector<string> loadExpected(const string& filename, vector<string>& inputs) {
    vector<string> outputs;
    ifstream file(filename);
    string line;
    while (getline(file, line)) {
        size_t pos = line.find(' ');
        if (pos != string::npos) {
            inputs.push_back(line.substr(0, pos));
            outputs.push_back(line.substr(pos + 1));
        }
        else {
            outputs.push_back(line);
        }
    }
    return outputs;
}

vector<string> loadActual(const string& filename) {
    vector<string> outputs;
    ifstream file(filename);
    string line;
    while (getline(file, line)) {
        outputs.push_back(line);
    }
    return outputs;
}

TestResult compareOutputs(int testNum, const string& input, const string& expected, const string& actual) {
    TestResult result;
    result.testNumber = testNum;
    result.input = input;
    result.expected = expected;
    result.actual = actual;
    result.passed = (expected == actual);

    if (!result.passed) {
        size_t minLen = min(expected.length(), actual.length());
        for (size_t i = 0; i < minLen; i++) {
            if (expected[i] != actual[i]) {
                result.errorPositions.push_back(i);
            }
        }
    }
    return result;
}

void printResult(const TestResult& r) {
    if (r.passed) {
        cout << "Test " << r.testNumber << ": OK";
        if (!r.input.empty()) cout << " [" << r.input << "]";
        cout << endl;
    }
    else {
        cout << "Test " << r.testNumber << ": FAIL" << endl;
        if (!r.input.empty()) cout << "  in:  " << r.input << endl;
        cout << "  exp: " << r.expected << endl;
        cout << "  got: " << r.actual << endl;
        if (r.expected.length() != r.actual.length()) {
            cout << "  len mismatch" << endl;
        }
        if (!r.errorPositions.empty()) {
            cout << "  pos:";
            for (int pos : r.errorPositions) {
                cout << " " << (pos + 1);
            }
            cout << endl;
        }
    }
}

void printSummary(const vector<TestResult>& results) {
    int passed = 0;
    for (auto& r : results) if (r.passed) passed++;
    int total = results.size();
    cout << "SUMMARY: " << passed << "/" << total << " passed" << endl;
}

int main(int argc, char* argv[]) {
    if (argc < 3) return 1;

    string expectedFile = argv[1];
    string actualFile = argv[2];

    vector<string> inputs;
    vector<string> expected = loadExpected(expectedFile, inputs);
    vector<string> actual = loadActual(actualFile);

    size_t n = min(expected.size(), actual.size());
    vector<TestResult> results;
    for (size_t i = 0; i < n; i++) {
        TestResult r = compareOutputs(i + 1,
            (i < inputs.size() ? inputs[i] : ""),
            expected[i],
            actual[i]);
        printResult(r);
        results.push_back(r);
    }
    printSummary(results);

    return 0;
}
