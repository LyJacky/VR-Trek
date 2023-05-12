set src=./
set dest=./
call uglifyjs "%src%main.js" -o "%dest%main.js"
call uglifyjs "%src%common.js" -o "%dest%common.js"
call uglifyjs "%src%0.js" -o "%dest%0.js"
call uglifyjs "%src%1.js" -o "%dest%1.js"
call uglifyjs "%src%2.js" -o "%dest%2.js"
call uglifyjs "%src%3.js" -o "%dest%3.js"
call uglifyjs "%src%polyfills.js" -o "%dest%polyfills.js"
call uglifyjs "%src%runtime.js" -o "%dest%runtime.js"
call uglifycss "%src%styles.css" --output "%dest%styles.css"