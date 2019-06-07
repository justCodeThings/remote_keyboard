from socket import AF_INET, socket, SOCK_STREAM
from threading import Thread
import json
import pyautogui
import time

server = socket(AF_INET, SOCK_STREAM)
host = ''
port = 11003
bufsiz = 1024
server.bind((host, port))
server.listen(5)
clients = {}
addresses = {}
data = {}
width, height = pyautogui.size()
pyautogui.FAILSAFE = False

def accept_incoming_connections():
    while True:
        client, client_address = server.accept()
        addresses[client_address] = client_address
        clients[client] = client
        print("{} has connected.".format(client_address))
        Thread(target=handle_client, args=(client, client_address)).start()

def handle_client(client, client_address):
    while True:
        
        try:
            msg = client.recv(bufsiz)
            msg = (msg).decode("utf8")
            data = json.loads(msg)

            x = int(data['x'])
            y = int(data['y'])
            left_click = data['left_click']
            right_click = data['right_click']

            broadcast(bytes("{},{}".format(width, height), "utf8"))
            
            print(str(client_address)+" drag: "+str(data['drag']))
            print(bool(data['left_click']))

            pyautogui.moveTo(x,y)

            mouseClick(left_click)
                
            if(data['right_click'] == 'True'):
                pyautogui.click(button= "right")
                
            '''if(data['key'] != 'false'):
                pyautogui.press(data['key'])'''
            timeout = time.time()
                
        except Exception as e:
            #print(e)
            if time.time()- timeout > 1:
                delete_client(client, client_address)
            #break

def delete_client(client, client_address):
    try:
        client.close()
        del addresses[client_address]
        del clients[client]
        broadcast(bytes("{} has disconnected.".format(client_address), "utf8"))
        print("{} has disconnected.".format(client_address))
    except Exception:
        pass

def broadcast(msg, prefix=""):
        for sock in clients:
            sock.send(bytes(prefix, "utf8")+msg)

def mouseClick(click):
    while click == 'True':
        drag = time.time()
        if time.time() - drag > 1:
            if left_click == 'False':
                pyautogui.click(button = "left")
                left_click = 'False'
            else:
                pyautogui.dragTo(x, y)
            

if __name__ == "__main__":
    print("waiting for connections...")
    accept_thread = Thread(target=accept_incoming_connections)
    accept_thread.start()
    accept_thread.join()
