from socket import AF_INET, socket, SOCK_STREAM
from threading import Thread
import json
import pyautogui

server = socket(AF_INET, SOCK_STREAM)
host = ''
port = 11002
bufsiz = 1024
server.bind((host, port))
server.listen(5)
clients = {}
addresses = {}
data = {}

def accept_incoming_connections():
    while True:
        client, client_address = server.accept()
        addresses[client_address] = client_address
        clients[client] = client
        print("client has connected." )
        #broadcast(bytes("%s has connected."%(client_address), "utf8"))
        Thread(target=handle_client, args=(client, client_address)).start()

def handle_client(client, client_address):
    while True:
        try:
            msg = client.recv(bufsiz)
            msg = (msg).decode("utf8")
            data = json.loads(msg)
            
            print(str(client_address)+": "+str(data['left_click']))
            print(bool(data['left_click']))

            pyautogui.moveTo(int(data['x']), int(data['y']))

            if(data['left_click'] == 'True'):
                pyautogui.click(button = "left")
                
            if(data['right_click'] == 'True'):
                pyautogui.click(button= "right")
                
            '''if(data['key'] != 'false'):
                pyautogui.press(data['key'])'''
                
        except Exception as e:
            print(e)
            #delete_client(client, client_address)
            #break

def delete_client(client, client_address):
    try:
        client.close()
        del addresses[client_address]
        del clients[client]
        #broadcast(bytes("%s has disconnected."%(client_address), "utf8"))
        print("client has disconnected.")
    except Exception:
        pass

def broadcast(msg, prefix=""):
        for sock in clients:
            sock.send(bytes(prefix, "utf8")+msg)

if __name__ == "__main__":
    print("waiting for connections...")
    accept_thread = Thread(target=accept_incoming_connections)
    accept_thread.start()
    accept_thread.join()
