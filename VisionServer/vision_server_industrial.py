import base64, json, socket, threading, cv2, numpy as np

HOST='0.0.0.0'; PORT=5001

def inspect(req):
    raw=base64.b64decode(req['image_base64']); img=cv2.imdecode(np.frombuffer(raw,np.uint8),cv2.IMREAD_COLOR)
    if img is None: return {'success':False,'inspectionPass':False,'message':'NO IMAGE','ngCode':'E003','count':0,'marks':[],'annotatedImageBase64':''}
    r=req.get('recipe',{}); gray=cv2.cvtColor(img,cv2.COLOR_BGR2GRAY); mode=r.get('threshold_mode','fixed')
    if mode=='otsu': _,bw=cv2.threshold(gray,0,255,cv2.THRESH_BINARY+cv2.THRESH_OTSU)
    elif mode=='adaptive': bw=cv2.adaptiveThreshold(gray,255,cv2.ADAPTIVE_THRESH_GAUSSIAN_C,cv2.THRESH_BINARY,31,5)
    else: _,bw=cv2.threshold(gray,int(r.get('threshold',128)),255,cv2.THRESH_BINARY)
    contours,_=cv2.findContours(bw,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE); min_a=float(r.get('min_area',1000)); max_a=float(r.get('max_area',500000)); marks=[]; out=img.copy()
    for c in contours:
        a=cv2.contourArea(c)
        if a<min_a or a>max_a: continue
        (x,y),(w,h),ang=cv2.minAreaRect(c)
        if w<h: ang+=90
        marks.append({'x':float(x),'y':float(y),'angle':float(ang),'area':float(a)})
        box=cv2.boxPoints(((x,y),(w,h),ang)); box=np.intp(box); cv2.drawContours(out,[box],0,(0,255,0),2); cv2.circle(out,(int(x),int(y)),4,(0,0,255),-1)
    passed=len(marks)>0; msg='PASS' if passed else 'COUNT NG - no valid target'; code='' if passed else 'V101'
    cv2.putText(out,msg,(15,30),cv2.FONT_HERSHEY_SIMPLEX,0.8,(0,255,0) if passed else (0,0,255),2)
    ok,jpg=cv2.imencode('.jpg',out); ann=base64.b64encode(jpg.tobytes()).decode() if ok else ''
    return {'success':True,'inspectionPass':passed,'message':msg,'ngCode':code,'count':len(marks),'marks':marks,'annotatedImageBase64':ann}

def client(conn):
    try:
        with conn.makefile('rwb') as f:
            line = f.readline()

            if not line:
                return

            req = json.loads(line.decode('utf-8'))

            command = req.get('command')

            if command == 'ping':
                resp = {
                    'success': True,
                    'message': 'pong'
                }

            elif command == 'inspect':
                resp = inspect(req)

            else:
                resp = {
                    'success': False,
                    'inspectionPass': False,
                    'message': 'UNKNOWN COMMAND',
                    'ngCode': 'V901',
                    'count': 0,
                    'marks': [],
                    'annotatedImageBase64': ''
                }

            data = json.dumps(
                resp,
                separators=(',', ':')
            ) + '\n'

            f.write(data.encode('utf-8'))
            f.flush()

    except Exception as e:
        print(f"[CLIENT ERROR] {type(e).__name__}: {e}", flush=True)

        try:
            error_resp = {
                'success': False,
                'inspectionPass': False,
                'message': str(e),
                'ngCode': 'V900',
                'count': 0,
                'marks': [],
                'annotatedImageBase64': ''
            }

            conn.sendall(
                (json.dumps(error_resp) + '\n').encode('utf-8')
            )
        except Exception as send_error:
            print(f"[SEND ERROR] {send_error}", flush=True)

    finally:
        try:
            conn.close()
        except:
            pass

def main():
    with socket.socket() as s:
        s.setsockopt(socket.SOL_SOCKET,socket.SO_REUSEADDR,1); s.bind((HOST,PORT)); s.listen(20); print(f'Industrial Vision Server listening on {HOST}:{PORT}')
        while True:
            c,_=s.accept(); threading.Thread(target=client,args=(c,),daemon=True).start()
if __name__=='__main__': main()
