# Introduction

EZNet is a tiny low overhead networking C# codebase. Aimed at developers
comfortable with programming who don't want to deal with boilerplate code.

Ready built for the Unity Engine. Core networking code works for any C# program.

# Features

- TCP & UDP socket client/server model.
- TCP used for commands, important data transfers.
- UDP used for fast as possible data transfers.
- Multithreaded Server & Client.
- Minimal packet overhead, no lazy serialization. 
- Create your own custom packet structures.
- Bind scene Objects & Scripts to networked data.
- Ready Made example and usage code for Unity.

# Quick Start : Demo Scene Localhost Test

1. Build the included ClientScene scene into a standalone.
2. Load ServerScene and run the game.
3. Press 'S' to start the server
4. Run the standalone client
5. Press 'C' to connect the client 
6. Move the Cube object on the server, transform state will be reproduced on client.

# Usage 

## Part 1 : Packet Structures

The core of this codebase is the idea of you, the game developer, creating your own catered packet structures.
This ensures lowest possible latency, lowest possible overhead, and maximum control over data exchanges.

Custom packet structures are essentially just classes that hold a bunch of variables you want to send over the network.
The class then implements an interface used to expose some universal data needed to send your custom structures using sockets.

The asset already contains two basic packet structures : NetCMD and NetTransform.

NetCMD is a packet structure for string commands. These are used to exchange important information.
The idea of a command is simply to pass a combintation of special characters and terms indicating different actions.
For instance, since the UDP port of clients is customizable, it must be sent to the server using the "/udpinit" command.
Arguments are added after the command. Following the example, the command to use UDP port 7002 would be "/udpinit 7002".

NetTransform is a packet structure for GameObject Transforms. 
This class contains 9 floating point values representing the position, rotation and scale of a game object transform. 
This is NOT a replacement for the unet network transform. More on this later.

These two packet structure classes implement the IPacket interface. This interface contains the following methods:

	    byte GetPacketType();		Returns structure TYPE value, ex : NetData.TYPE_CMD or NetData.TYPE_NETTRANSFORM

        byte GetID();				Returns data store ID value, ex : NetData.ID_NETTRANSFORM.TestTransform

        short GetLength();			Returns the length of the array that will be returned by EncodeRaw()

        byte[] EncodeRaw();			Main encoding step. Take class variables and shove them into a byte array.

        void DecodeRaw(byte[] raw);	Main decoding step. Fill class variables with the input byte array.

In order to create and add your custom packet structures to the codebase : 

	1. Create a new class and implement this interface :

		A. Native types can be turned into byte arrays using the .NET BitConverter
		B. Classes must be decomposed into native types, then do step A with those.
		C. Using Array.ConstrainedCopy() pack everything into a final byte array
		D. Return this array at the end of the EncodeRaw() implementation

		E. At reverse, byte arrays can be turned into native types using BitConverter
		F. Same with classes, can be rebuilt using the needed native types.
		G. Be careful to respect the same order when encoding and decoding

	2. Go into NetPacket.cs and find the NetData class.
	3. Create a new TYPE_MYTYPE style byte value. Make it unique relative to other field with the same naming scheme.
	4. Create a new ID_MYTYPE style enum. This is where you create IDs to decide which objects receive which data.
	5. Create a new array of your packet structure. (just like the public NetCMD[] CMD; line)
	6. Go inside the NetData constructor
	7. Initialize the created array
	8. Loop over it and initialize the object slots (prevents NullReferenceException)
	9. Go back into your custom packet structure and finish implementing the interface, filling in with the new Type and ID enums.


You now have created and added a new Packet Structure to the codebase and it is ready to send and receive.
As you can see, there is no universal serialization in order to avoid the extra overhead. 
Besides the byte array returned by EncodeRaw(), packets must be Pack()'d with 5 bytes of header data : 

	First byte represents the CLIENT ID value. This is a unique client identifier used by the server to know who sent a packet.
	Second byte represents the TYPE value. This is the value returned by GetPacketType(). Used to correctly read packets.
	Third byte represents the ID value. This is the value returned by GetID(). Used to know which object the packet represents.
	Finally the fourth and fifth bytes are used to store the short typed LENGTH returned by GetLength()

The Pack() function takes in a CID value and a IPacket type parameter, which means any class implementing IPacket can be passed.
This function returns a final byte array that is now ready to send using either TCP or UDP. Nothing else is added.
 
There is nothing to change about this function since it uses the data exposed by the implemented interface to build headers.
If you need to change header data structure, see the GenerateHeader() method.


## Part 2 : Bindings


Bingings are Monobehaviors responsible for linking game objects and components to networked data.

Bindings operate in three modes : NONE, READ or WRITE

    The NONE mode is used to disable binding behaviors.
    The READ mode is used to get bound object data and update the relevant NetData.
    The WRITE mode is used to get NetData and apply it to the bound object.

To create a custom game object binding, create a class and have it implement the INetBinding interface.
To get help correctly implementing the interface, refer to the example TransformBinding.cs class.

A NetBinding class must contain the following fields : 

	1. A NetBindingMode field representing either the NONe, READ or WRITE binding behavior.
	2. A Custom NetData.ID enum field. In the TransformBinding.cs example, this field uses the NetData.ID_NETTRANSFORM enum.
	
This second step exposes a drop down menu in the inspector which can be used to pick what 
NetData ID the game object will be reading data from in the networked data store.

You can then fill in the implemented method to :

	GetBindingDataID() returns the Custom Data Type ID that will be set using the inspector
	GetBindingMode() and SetBindingMode() return and set the Binding Mode field value. 
	GetBindingDataType() returns the NetData.TYPE_ representing which type of NetData this binding reads or writes.
	
And finally the SyncBinding() function, which should switch() over the mode variable to determine what to do next.
If the mode is set to READ, you should then take some data from GameObject Components and write it to BindingUtils.datastore
This works in the opposite direction if the mode is set to WRITE. The NONE mode should break out of switch without doing anything.

The BindingUtils.datastore is a direct reference to the datastore used by either NetClient or NetServer.
From this reference you can access data arrays of a certain NetData.TYPE_ and cast the ID as a byte to use as the index.

It is suggested to take a closer look at the implementation of the TransformBinding's SyncBinding() function.
The priciple is very easy to understand, as it shows how a NetTransform relates to the actual GameObject's transform.

Finally, to finish up a NetBinding class, add the following code into the Monobehavior Update() method : 

    if (BindingUtils.Ready)
    {
        SyncBinding();
    }
	
This code will wait until the BindingUtils datastore reference is loaded before starting to sync the binding.
Without this the Update() method will execute before the NetClient or NetServer instance is created and throw NullReferenceExceptions

## Part 3 : Server & Client Implementations

You are almost ready to start using the created packet structure. 
Inside the NetClient and/or NetServer class (this depends on what you need) :

	1. 	Create a new SendMyData() style function. Add any parameters you need, such as Client ID.
		This function can then instantiate a NetPacket class and set the CID to the input parameter.
		The important part is to then set the NetPacket.data value to the result of PacketUtils.Pack()
		And finally use either the TCPOut or UDPOut threaded queues to send using TCP or UDP.

	2. 	Add a new case inside the OnReceive() function for your NetData.TYPE 
		What you do next is case dependent, however the idea is to create an instance of your packet structure class.
		Then calling the MyPacketStructure.DecodeRaw() function passing in the p.data array received from the network.
		Finally, either directly update the datastore using the data stored in phtemp to set data for the correct ID.
		Or create a custom OnMyPacketStructureDataReceived() style function in which you can customize what happens.
		This is where you would decide on things such as server authority on certain values, authorized commands, validate input, etc.
		
You are now done implementing your new custom packet structure.

# Tips 

## NetData Datastore

The NetData class instance on NetClient or NetServer acts as a common storage class for data.
Keep in mind that despite the two NetPacket examples being thread safe, your own structures may not be.
Net Threads writing and bindings reading at the same time may cause inconsistent behavior.
In cases there the datastore causes problems between threads, use lock() or volatile fields.

## Interpolation

NetTransforms allow for interpolation of position and rotation parameters. 
The implementation relies on a set server tickrate in order to LERP between the two last received transforms.
You can see the effect in action by either lowering the tickrate or moving the test cube in an immediate manner on the server.
The client will see a smoothed out version of the movement experience on the server. Note that this does not fully recreate movements.
For instance if a rotation somewhat exceeds 360 degrees the interpolation will make it look as it the object only rotated a small amount.

## Server Authority

Many different methods can be used to control server authority. One of these methods is to rely on a HashSet of netdata IDs.
The IDs added to the hashset are considered "Client Authoritative" which means clients can change this value on the server.
This is best in cases where particular IDs are attributed to particular clients.

Another way to deal with this is to filter NetData types & ids and to perform further actions than simply editing the datastore.
For instance clients can rely on a single common "SelfTransform" ID to send their own position and the server can 
sort based on Client ID and put the data into a client specific datastore. 
